﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Data.Entity.Core;
using System.Data.Entity.Validation;
using ProjektBD.Utilities;
using ProjektBD.Model;
using ProjektBD.Controllers;
using System.Data.Entity.Infrastructure;
using System.Reflection;

namespace ProjektBD.Forms
{
    public partial class AdministratorMain : Form
    {
        #region Pola i konstruktor
        //----------------------------------------------------------------

        /// <summary>
        /// Warstwa pośrednicząca między widokiem a modelem (bazą danych). Przetwarza i oblicza
        /// </summary>
        private AdminController formController;

        /// <summary>
        /// Dtruktura z powiadomieniami dla admina
        /// </summary>
        private AdminNotifications notifications;

        /// <summary>
        /// Login zalogowanego admina.
        /// </summary>
        private string userLogin;

        public AdministratorMain(string inputLogin)
        {
            InitializeComponent();

            formController = new AdminController(inputLogin);
            userLogin = inputLogin;        
        }

        //----------------------------------------------------------------
        #endregion

        #region Ładowanie formularza
        //----------------------------------------------------------------

        private void AdministratorMain_Load(object sender, EventArgs e)
        {
            if (formController.connectToDatabase())
                this.Close();

            if (EmergencyMode.isEmergency)
            {
                label4.ForeColor = Color.Crimson;
                label4.Text = "wyłączona";
            }

            else
            {
                label4.ForeColor = Color.Chartreuse;
                label4.Text = "włączona";
            }

            lookForNewTeachers();

            new ToolTip().SetToolTip(pictureBox2, "Wyloguj");

            comboBox1.Items.AddRange(formController.getTableNames());
            comboBox2.Items.AddRange(formController.getInstituteNames());

            List<ProwadzącyDTO> list = formController.getTeachers();

            customListView1.fill<ProwadzącyDTO>(list);
        }

        //----------------------------------------------------------------
        #endregion

        #region Tryb awaryjny
        //----------------------------------------------------------------

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            pictureBox1.Image = ProjektBD.Properties.Resources.pressed;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            pictureBox1.Image = ProjektBD.Properties.Resources.unpressed;

            try
            {
                formController.changeEmergencyMode();

                if (EmergencyMode.isEmergency)
                {
                    label4.ForeColor = Color.Crimson;
                    label4.Text = "wyłączona";
                }
                else
                {
                    label4.ForeColor = Color.Chartreuse;
                    label4.Text = "włączona";
                }
            }

            catch (EntityException)
            {
                MsgBoxUtils.displayConnectionErrorMsgBox();
            }
        }

        //----------------------------------------------------------------
        #endregion

        #region DataGrid
        //----------------------------------------------------------------

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string tableName = comboBox1.Text;

            var query = formController.getTableData(tableName);

            godlyDataGrid1.provideParams(formController, query.Count);

            // Naprawia mały bug edycji przy przechodzeniu np. z Projektu do Oceny (ProjektID wciąż był tylko do odczytu)
            godlyDataGrid1.DataSource = null;
            godlyDataGrid1.DataSource = query;

            if ( query.GetType().Name.Contains("Observable") )          // Jeśli zwrócona kolekcja jest obserwowalna, znaczy to, iż mamy ją w kontekście
            {
                label7.Visible = false;

                List<string> keysList = formController.getPrimaryKeyNames(tableName);

                keysList.ForEach(keyName => godlyDataGrid1.Columns[keyName].ReadOnly = true);
            }
            else
            {
                label7.Visible = true;

                foreach (DataGridViewColumn column in godlyDataGrid1.Columns)
                    column.ReadOnly = true;
            }
        }

        //----------------------------------------------------------------
        #endregion

        #region Przypisywanie do zakładu
        //----------------------------------------------------------------

        private void button1_Click(object sender, EventArgs e)
        {
            string instituteName = comboBox2.Text;
            string teacherLogin = "";

            if (customListView1.SelectedItems.Count > 0)
                teacherLogin = customListView1.SelectedItems[0].Text;

            switch ( formController.assignToInstitute(instituteName, teacherLogin) )
            {
                case "Nie podano zakładu":
                    MsgBoxUtils.displayWarningMsgBox("Błąd", "Proszę wybrać zakład z listy");
                    break;

                case "Nie wybrano prowadzącego":
                    MsgBoxUtils.displayWarningMsgBox("Błąd", "Proszę wybrać prowadzącego z listy");
                    break;

                case "Przypisanie przebiegło pomyślnie":
                    MsgBoxUtils.displayInformationMsgBox("Informacja", "Prowadzący został pomyślnie przypisany do zakładu");

                    List<ProwadzącyDTO> list = formController.getTeachers();
                    customListView1.fill<ProwadzącyDTO>(list);                  // refresh
                    break;
            }
        }

        //----------------------------------------------------------------
        #endregion

        #region Akceptowanie prowadzących
        //----------------------------------------------------------------

        #region Metody
        //----------------------------------------------------------------

        /// <summary>
        /// Odświeża informacje o nowych notyfikacjach. Teraz tylko 
        /// szuka nowych userów ubiegających się o prowadzącego.
        /// </summary>
        private void lookForNewTeachers()
        {
            try
            {
                notifications.newUsers = formController.findNewUsers();
                notifications.newUsersCount = notifications.newUsers.Count;

                if (notifications.newUsersCount != 0)
                {
                    notificationImage.Image = ProjektBD.Properties.Resources.znak;
                    notificationCount.Visible = true;

                    if (notifications.newUsersCount <= 100)
                        notificationCount.Text = notifications.newUsersCount.ToString();
                    else
                        notificationCount.Text = "99+";
                }
                else
                {
                    notificationImage.Image = ProjektBD.Properties.Resources.znak2;
                    notificationCount.Visible = false;
                }
            }

            catch (EntityException)
            {
                MsgBoxUtils.displayConnectionErrorMsgBox();
            }
        }

        /// <summary>
        /// Wywołanie msgBoxa dla nowego użytkownika ubiegającego się o prowadzącego.
        /// Można go akceptować (Tak), odrzucić (Nie) lub wybrać później (Anuluj)
        /// </summary>
        /// <param name="u">Rozpatrywany Użytkownik</param>
        private void acceptNewTeacher(Użytkownik u)
        {
            switch (MessageBox.Show("Nowy użytkownik:\n\tLogin: " + u.login + "\n\tE-mail: " + u.email + "\n Ubiega się o uprawnienia prowadzącego. Akceptować?", "Nowy prowadzący",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3))
            {
                case DialogResult.Yes:
                    formController.addTeacher(u);
                    break;

                case DialogResult.No:
                    formController.deleteUser(u);
                    break;

                default:
                    break;
            }
        }

        //----------------------------------------------------------------
        #endregion

        #region Eventy
        //----------------------------------------------------------------

        /// <summary>
        /// Menu kontekstowe również pod LPM
        /// </summary>
        private void notificationImage_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show(Control.MousePosition);
        }

        /// <summary>
        /// Menu kontekstowe również pod LPM
        /// </summary>
        private void notificationCount_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show(Control.MousePosition);
        }
        /// <summary>
        /// Poinformuj ile użytkowników ubiega się o prowadzącego.
        /// W przyszłości może być więcej rodzajów notek, wtedy modyfikujemy dalsze menuItemy.
        /// </summary>
        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (notifications.newUsersCount > 0)
            {
                string str = "";

                str += notifications.newUsersCount.ToString() + " nowy" + ((notifications.newUsersCount > 1) ? "ch" : "") + " użytkownik" + ((notifications.newUsersCount > 1) ? "ów" : "");

                nowyUserToolStripMenuItem.Text = str;
                nowyUserToolStripMenuItem.ForeColor = Color.Red;
            }
            else
            {
                nowyUserToolStripMenuItem.Text = "Brak nowych użytkowników";
                nowyUserToolStripMenuItem.ForeColor = Color.Black;
            }
        }

        /// <summary>
        /// Dynamicznie ładuje listę nowych użytkowników ubiegających się o prowadzącego
        /// </summary>
        private void nowyUserToolStripMenuItem_MouseEnter(object sender, EventArgs e)
        {
            nowyUserToolStripMenuItem.DropDownItems.Clear();
            List<ToolStripMenuItem> items = new List<ToolStripMenuItem>();

            try                            // dopiero tutaj, a nie wewnątrz funkcji, ponieważ połączenie może się zerwać w połowie dodawania prowadzących
            {
                for (int i = 0; i < (notifications.newUsersCount > 10 ? 10 : notifications.newUsersCount); i++) // do 10, żeby menu się nie rozrastało niepotrzebnie
                {
                    ToolStripMenuItem tmp = new ToolStripMenuItem();
                    tmp.Text = notifications.newUsers[i].login;
                    items.Add(tmp);
                    tmp.Click += new EventHandler(MenuItemClickHandler);
                }

                nowyUserToolStripMenuItem.DropDownItems.AddRange(items.ToArray());
                nowyUserToolStripMenuItem.DropDown.AllowDrop = true;
            }

            catch (EntityException)
            {
                MsgBoxUtils.displayConnectionErrorMsgBox();
            }
        }

        /// <summary>
        /// Identyfikuje prowadzącego na podstawie przyciśniętego MenuItema
        /// </summary>
        private void MenuItemClickHandler(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;

            foreach (Użytkownik u in notifications.newUsers)
            {
                if (clickedItem.Text == u.login)
                    acceptNewTeacher(u);
            }

            lookForNewTeachers();
        }

        //----------------------------------------------------------------
        #endregion

        //----------------------------------------------------------------
        #endregion

        #region Wiadomości
        //----------------------------------------------------------------

        private void messageImage_Click(object sender, EventArgs e)
        {
            openCommunicator();
        }

        private void messageCount_Click(object sender, EventArgs e)
        {
            openCommunicator();
        }

        /// <summary>
        /// Metoda otwierająca komunikator lub uaktywniająca go, gdy został już wcześniej otworzony
        /// </summary>
        private void openCommunicator()
        {
            var openedGGForms = Application.OpenForms.OfType<Komunikator>().ToList();

            // Blokuje możliwość otwarcia drugiego komunikatora
            if (openedGGForms.Count == 0)
            {
                Komunikator form = new Komunikator(userLogin);
                form.Show();
            }
            else
            {
                if (openedGGForms[0].WindowState == FormWindowState.Minimized)
                    openedGGForms[0].WindowState = FormWindowState.Normal;
                else
                    openedGGForms[0].Activate();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            checkForNewMessages();
        }

        /// <summary>
        /// Szuka nowych wiadomości zaadresowanych do użytkownika
        /// </summary>
        private void checkForNewMessages()
        {
            int newMessagesCount = formController.getNewMessagesCount();

            if (newMessagesCount > 0)
            {
                messageImage.Image = ProjektBD.Properties.Resources.mail;
                messageCount.Visible = true;

                if (newMessagesCount <= 100)
                    messageCount.Text = newMessagesCount.ToString();
                else
                    messageCount.Text = "99+";
            }
            else
            {
                messageImage.Image = ProjektBD.Properties.Resources.mail2;
                messageCount.Visible = false;
            }
        }

        //----------------------------------------------------------------
        #endregion

        #region Help i zarządzanie kontem
        //----------------------------------------------------------------

        /// <summary>
        /// Wyświetlanie "About"
        /// </summary>
        private void toolStripLabel3_Click(object sender, EventArgs e)
        {
            HelpFormStrategy.chooseHelpFormStrategy(HelpFormTypes.About);
        }

        /// <summary>
        /// Wyświetlanie pomocy
        /// </summary>
        private void toolStripLabel2_Click(object sender, EventArgs e)
        {
            HelpFormStrategy.chooseHelpFormStrategy(HelpFormTypes.Admin);
        }

        /// <summary>
        /// Otwieranie zarządzania kontem.
        /// </summary>
        private void toolStripLabel1_Click(object sender, EventArgs e)
        {
            Zarządzanie_Kontem newForm = new Zarządzanie_Kontem(userLogin);
            newForm.ShowDialog();
            newForm.Dispose();
        }

        /// <summary>
        /// Wyświetlanie pomocy
        /// </summary>
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            HelpFormStrategy.chooseHelpFormStrategy(HelpFormTypes.Admin);
        }

        //----------------------------------------------------------------
        #endregion

        #region Zamykanie formularza
        //----------------------------------------------------------------

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Zamykanie formatki - messageBox z zapytaniem.
        /// </summary>
        private void AdministratorMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.WindowsShutDown)
                return;

            DialogResult result = MsgBoxUtils.displayQuestionMsgBox("Wyjdź", "Czy na pewno chcesz się wylogować?", this);

            if (result == DialogResult.No)
                e.Cancel = true;
        }

        /// <summary>
        /// Zamknięcie formatki - Pozbywa się utworzonego kontekstu przy zamykaniu formularza
        /// </summary>
        private void AdministratorMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            formController.disposeContext();
        }

        //----------------------------------------------------------------
        #endregion
    }

    /// <summary>
    /// Struktura z powiadomieniami dla admina.
    /// </summary>
    struct AdminNotifications
    {
        public List<Użytkownik> newUsers;
        public int newUsersCount;
    }
}