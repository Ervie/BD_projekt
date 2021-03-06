﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektBD.Model
{
    class Użytkownik
    {
        public Użytkownik()
        {
            Rozmowy = new HashSet<Rozmowa>();
        }

        public int UżytkownikID { get; set; }               // Primary Key

        [MaxLength(50)]
        [Required]
        public string login { get; set; }

        [MaxLength(64)]
        [Required]
        public string hasło { get; set; }

        [MaxLength(32)]
        [Required]
        public string sól { get; set; }

        [MaxLength(50)]
        [Required]
        public string email { get; set; }

        [MaxLength(100)]
        public string miejsceZamieszkania { get; set; }

        public DateTime? dataUrodzenia { get; set; }

        [Browsable(false)]
        public virtual ICollection<Rozmowa> Rozmowy { get; set; }
    }
}
