﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProjektBD.Model;

namespace ProjektBD.Databases
{
    class customListViewDatabase : DatabaseBase
    {
        /// <summary>
        /// Pobiera informacje o profilu studenta z bazy
        /// </summary>
        public StudentProfileDTO getStudentProfileData(int nrIndeksu)
        {
            var query = from s in context.Studenci
                        where s.nrIndeksu == nrIndeksu
                        select new StudentProfileDTO
                        {
                            login = s.login,
                            nrIndeksu = s.nrIndeksu,
                            email = s.email,
                            miejsceZamieszkania = s.miejsceZamieszkania,
                            dataUrodzenia = s.dataUrodzenia
                        };

            return query.Single();
        }

        /// <summary>
        /// Pobiera informacje o profilu studenta z bazy
        /// </summary>
        public StudentProfileDTO getStudentProfileData(string studentLogin)
        {
            var query = from s in context.Studenci
                        where s.login.Equals(studentLogin)
                        select new StudentProfileDTO
                        {
                            login = s.login,
                            nrIndeksu = s.nrIndeksu,
                            email = s.email,
                            miejsceZamieszkania = s.miejsceZamieszkania,
                            dataUrodzenia = s.dataUrodzenia
                        };

            return query.Single();
        }

        /// <summary>
        /// Pobiera z bazy informacje o profilu prowadzącego dany przedmiot
        /// </summary>
        public TeacherProfileDTO getTeacherProfileFromSubject(string subjectName)
        {
            var query = from prow in context.Prowadzący
                            join subj in context.Przedmioty on prow.UżytkownikID equals subj.ProwadzącyID
                        where subj.nazwa.Equals(subjectName)
                        select new TeacherProfileDTO
                        {
                            login = prow.login,
                            email = prow.email,
                            miejsceZamieszkania = prow.miejsceZamieszkania,
                            dataUrodzenia = prow.dataUrodzenia,
                            nazwaZakładu = prow.Zakład.nazwa                // Sprawdź, czy Ci działa, Ervi ;p
                        };

            return query.Single();
        }

        /// <summary>
        /// Pobiera z bazy informacje o profilu prowadzącego
        /// </summary>
        public TeacherProfileDTO getTeacherProfileData(string teacherLogin)
        {
            var query = from prow in context.Prowadzący
                        where prow.login.Equals(teacherLogin)
                        select new TeacherProfileDTO
                        {
                            login = prow.login,
                            email = prow.email,
                            miejsceZamieszkania = prow.miejsceZamieszkania,
                            dataUrodzenia = prow.dataUrodzenia,
                            nazwaZakładu = prow.Zakład.nazwa                // Tu też
                        };

            return query.Single();
        }

        /// <summary>
        /// Pobiera z bazy informacje o przedmiocie
        /// </summary>
        public PrzedmiotDetailsDTO getSubjectDetails(string subjectName)
        {
            var query = from subj in context.Przedmioty
                            join ob in context.PrzedmiotyObieralne on subj.PrzedmiotID equals ob.PrzedmiotID into obier

                        from allSubjects in obier.DefaultIfEmpty()          // LEFT JOIN
                        where subj.nazwa.Equals(subjectName)
                        select new PrzedmiotDetailsDTO
                        {
                            nazwa = subj.nazwa,
                            prowadzący = subj.Prowadzący.login,             // I tu
                            liczbaStudentów = subj.liczbaStudentów,
                            maxLiczbaStudentów = allSubjects.maxLiczbaStudentów,
                            opis = subj.opis
                        };

            return query.Single();
        }

        /// <summary>
        /// Pobiera z bazy informacje o projekcie
        /// </summary>
        public ProjektDetailsDTO getProjectDetails(string projectName)
        {
            var query = from proj in context.Projekty
                        where proj.nazwa.Equals(projectName)
                        select new ProjektDetailsDTO
                        {
                            nazwa = proj.nazwa,
                            nazwaPrzedmiotu = proj.Przedmiot.nazwa,
                            liczbaStudentów = proj.Studenci.Count,          // I tu
                            maxLiczbaStudentów = proj.maxLiczbaStudentów,
                            opis = proj.opis
                        };

            return query.Single();
        }

        /// <summary>
        /// Pobiera z bazy informacje o ocenie
        /// </summary>
        public OcenaDetailsDTO getGradeDetails(long gradeID)
        {
            var query = from grade in context.Oceny
                        where grade.OcenaID == gradeID
                        select new OcenaDetailsDTO
                        {
                            nazwaPrzedmiotu = grade.Przedmiot.nazwa,
                            nazwaProjektu = grade.Projekt.nazwa,
                            wartość = grade.wartość,
                            komentarz = grade.komentarz,
                            dataWpisania = grade.dataWpisania
                        };

            return query.Single();
        }

        /// <summary>
        /// Pobiera z bazy informacje o rozmowie
        /// </summary>
        public ConversationDetailsDTO getConversationDetails(int conversationID)
        {
            // Informacje o rozmowie - data rozpoczęcia i ilość wiadomości
            var conversationQuery = context.Database.SqlQuery<ConversationDetailsDTO>
                (@"
                        SELECT r.dataRozpoczęcia, COUNT(msg.WiadomośćID) AS ilośćWiadomości
                        FROM Rozmowa r
	                        LEFT JOIN Wiadomość msg ON r.RozmowaID = msg.RozmowaID
                        WHERE r.RozmowaID = " + conversationID + @"
                        GROUP BY r.dataRozpoczęcia"
                ).Single();

            // Użytkownicy biorący udział w rozmowie
            var conversationMembers = context.Database.SqlQuery<string>
                (@"
                        SELECT u.login
                        FROM Prowadzone_rozmowy pr
	                        JOIN Użytkownik u on pr.UżytkownikID = u.UżytkownikID
                        WHERE pr.RozmowaID = " + conversationID
                ).ToList();

            // Dodawanie rozmówców
            foreach (string member in conversationMembers)
                conversationQuery.rozmówcy += member + ", ";

            // Usunięcie przecinka na końcu listy rozmówców
            string tmp = conversationQuery.rozmówcy;
            conversationQuery.rozmówcy = tmp.Remove(tmp.Length - 2, 2);

            return conversationQuery;
        }
    }
}
