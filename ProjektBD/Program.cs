﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProjektBD.Forms;
using ProjektBD.Forms.CommonForms;

namespace ProjektBD
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new LoginForm());
            //Application.Run(new Komunikator("Forczu"));
            //Application.Run(new ProwadzacyMain("BLanuszny"));
            //Application.Run(new AdministratorMain("admin"));
        }
    }
}
