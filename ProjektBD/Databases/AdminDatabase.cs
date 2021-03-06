﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

using ProjektBD.Model;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core;
using ProjektBD.Utilities;

namespace ProjektBD.Databases
{
    /// <summary>
    /// Baza danych dla formularza administratora
    /// </summary>
    class AdminDatabase : DatabaseBase
    {
        #region Pola i konstruktor
        //----------------------------------------------------------------

        /// <summary>
        /// ID administratora
        /// </summary>
        private int userID;

        public AdminDatabase(string adminLogin)
        {
            userID = context.Administratorzy
                .Where( p => p.login.Equals(adminLogin) )
                .Select( p => p.UżytkownikID )
                .Single();
        }

        //----------------------------------------------------------------
        #endregion

        #region Dodawanie prowadzących
        //----------------------------------------------------------------

        /// <summary>
        /// Wyszukuje listę użytkowników, którzy nie są ani Studentami ani Prowadzącymi
        /// (czyli Ci starający się o Prowadzącego)
        /// </summary>
        /// <returns>Zwraca listę userów (nie będących studentami ani prowadzącymi)</returns>
        internal List<Użytkownik> findUsers()
        {
            var query = context.Database.SqlQuery<Użytkownik>("SELECT * " +
                                                            "FROM UŻYTKOWNIK u FULL OUTER JOIN STUDENT s " +
                                                            "ON u.UżytkownikID = s.UżytkownikID " +
                                                            "FULL OUTER JOIN PROWADZĄCY p " +
                                                            "ON u.UżytkownikID = p.UżytkownikID " +
                                                            "WHERE s.nrIndeksu IS NULL AND p.ZakładID IS NULL AND u.UżytkownikID >	1").ToList();

            return query;
        }

        /// <summary>
        /// Wywala z bazy podanego usera.
        /// </summary>
        /// <param name="u">Użytkownik usuwany z bazy</param>
        internal void deleteUser(Użytkownik u)
        {
            // Korzystamy z narzędzia ORM, dlatego jeśli zapytanie jest w miarę proste, lepiej starajmy się
            // korzystać z obiektowych mechanizmów

            //var command = @"DELETE FROM UŻYTKOWNIK WHERE UżytkownikID = @param";
            //context.Database.ExecuteSqlCommand(command, new SqlParameter("param", u.UżytkownikID));

            Użytkownik userToDelete = context.Użytkownicy.Where(s => s.UżytkownikID == u.UżytkownikID).FirstOrDefault();

            if (userToDelete != null)
            {
                context.Użytkownicy.Remove(userToDelete);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// "Zmienia" użytkownika w prowadzącego - usuwa użytkownika i dodaje go jako prowadzącego.
        /// Nadaje początkowy zakład: *nieznany*.
        /// </summary>
        /// <param name="u">Upgrade'owanmy użytkownik</param>
        internal void addTeacher(Użytkownik u)
        {
            if (context.Zakłady.Where(z => (z.nazwa.Equals("*nieznany*"))).ToList().Count < 1)
            {
                Zakład z = new Zakład { nazwa = "*nieznany*" };

                context.Zakłady.Add(z);
                context.SaveChanges();
            }

            Prowadzący p = new Prowadzący
            {
                login = u.login,
                hasło = u.hasło,
                sól = u.sól,
                email = u.email,
                miejsceZamieszkania = u.miejsceZamieszkania,
                ZakładID = 4
            };

            if (u.dataUrodzenia != null)                // data urodzenia domyślnie jest null, dlatego else zbędny
                p.dataUrodzenia = u.dataUrodzenia;

            deleteUser(u);                              // usuwamy użytkownika już teraz, by zrobił miejsce prowadzącemu

            // Modyfikuje licznik autoinkrementacji klucza głównego UżytkownikID
            // Dzięki temu świeżo dodanemu użytkownikowi zostanie przydzielony ID tego, który przed chwilą usunęliśmy
            // W zasadzie niepotrzebne, ale za to tabela ładniej wygląda :3
            //context.Database.ExecuteSqlCommand("DBCC CHECKIDENT('Użytkownik', RESEED, " + (u.UżytkownikID - 1) + ");");

            context.Prowadzący.Add(p);
            context.SaveChanges();
        }

        //----------------------------------------------------------------
        #endregion

        #region Obsługa dataGrid'a
        //----------------------------------------------------------------

        /// <summary>
        /// Pobiera nazwy tabel istniejących w bazie
        /// </summary>
        public List<string> getTableNames()
        {
            return context.Database.SqlQuery<string>("SELECT name FROM sys.tables ORDER BY name").ToList();
        }

        /// <summary>
        /// Pobiera wszystkie wiersze z tabeli o podanej nazwie
        /// </summary>
        /// <typeparam name="T">Typ encji, której elementy przechowywane będą w zwracanej liscie</typeparam>
        public IList getTableData<T>(string tableName) where T: class
        {
            DbSet<T> result = context.Set<T>();         // Tworzy nowy DbSet, który podpiernicza interesujące nas elementy z istniejącego kontekstu

            try
            {
                result.Load();                          // Wyrzuci wyjątek, jeśli w kontekście nie ma DbSet'u typu T

                return result.Local.ToBindingList();
            } 
            catch (InvalidOperationException) {}

            return context.Database.SqlQuery<T>("SELECT * FROM " + tableName).ToList();     // Zapytanie dla tabel, których nie ma w kontekście
        }

        /// <summary>
        /// Pobiera nazwy kluczy głównych z tabeli
        /// </summary>
        /// <typeparam name="T">Typ klasy encji, której klucza szukamy</typeparam>
        public List<string> getPrimaryKeyNames<T>() where T : class
        {
            var set = ( (IObjectContextAdapter)context ).ObjectContext.CreateObjectSet<T>();
            var entitySet = set.EntitySet;

            return entitySet.ElementType.KeyMembers.Select(k => k.Name).ToList();
        }
         
        /// <summary>
        /// Sprawdza, czy kontekst posiada nowe dane, które musi wysłać do bazy
        /// </summary>
        public bool doesContextHaveChanges()
        {
            return context.ChangeTracker.HasChanges();
        }

        //----------------------------------------------------------------
        #endregion

        #region Przypisywanie do zakładu
        //----------------------------------------------------------------

        /// <summary>
        /// Pobiera prowadzących z bazy
        /// </summary>
        public List<ProwadzącyDTO> getTeachers()
        {
            var teacherQuery =  from p in context.Prowadzący
                                    join z in context.Zakłady on p.ZakładID equals z.ZakładID
                                select new ProwadzącyDTO
                                {
                                    login = p.login,
                                    email = p.email,
                                    nazwaZakładu = z.nazwa
                                };

            return teacherQuery.ToList();
        }

        /// <summary>
        /// Pobiera nazwy zakładów istniejących w bazie
        /// </summary>
        public List<string> getInstituteNames()
        {
            return context.Zakłady.Select(n => n.nazwa).ToList();
        }

        /// <summary>
        /// Przypisuje prowadzącego do zakładu
        /// </summary>
        public void assignToInstitute(string instituteName, string teacherLogin)
        {
            Prowadzący teacher = context.Prowadzący.Where( p => p.login.Equals(teacherLogin) ).Single();
            Zakład institute = context.Zakłady.Where( z => z.nazwa.Equals(instituteName) ).Single();

            teacher.Zakład = institute;
            context.SaveChanges();
        }

        //----------------------------------------------------------------
        #endregion

        #region Wiadomości
        //----------------------------------------------------------------

        /// <summary>
        /// Pobiera z bazy ilość nieprzeczytanych wiadomości użytkownika
        /// </summary>
        public int getNewMessagesCount()
        {
            string userLogin = context.Użytkownicy
                .Where(u => u.UżytkownikID == userID)
                .Select(s => s.login)
                .Single();

            var query = context.Database.SqlQuery<int>(@"
                            SELECT COUNT(msg.WiadomośćID) AS ilość
                            FROM Wiadomość msg
	                            JOIN Prowadzone_rozmowy pr ON msg.RozmowaID = pr.RozmowaID
                            WHERE pr.UżytkownikID = " + userID + @" AND
	                            msg.nadawca != '" + userLogin + @"' AND
	                            msg.przeczytana = 0");

            return query.Single();
        }

        //----------------------------------------------------------------
        #endregion
    }
}