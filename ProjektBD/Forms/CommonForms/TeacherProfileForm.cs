﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ProjektBD.Model;

namespace ProjektBD.Forms.CommonForms
{
    // Nowa linijka: prowadzi przedmioty?
    public partial class TeacherProfileForm : Form
    {
        public TeacherProfileForm(TeacherProfileDTO profileInfo)
        {
            InitializeComponent();

            label2.Text = profileInfo.login;
            label3.Text = profileInfo.nazwaZakładu;
            label7.Text = profileInfo.email;

            if (profileInfo.miejsceZamieszkania != null)
                label9.Text = profileInfo.miejsceZamieszkania;
            else
                label9.Text = "Nie podano";

            if (profileInfo.dataUrodzenia.HasValue)
                label11.Text = profileInfo.dataUrodzenia.Value.ToLongDateString();      // Konwersja do daty bez godziny
            else
                label11.Text = "Nie podano";
        }
    }
}
