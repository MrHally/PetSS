﻿using PetCare.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace petCare.Models
{
    public class Thucung
    {
        [Key]
        public int id_pet { get; set; }
        [MaxLength(100)]
        public string ten_pet { get; set; } = "";
        [DisplayFormat(DataFormatString = "{0:dd:MM:yyyy}", ApplyFormatInEditMode = true)]
        public DateTime ngaysinh_pet { get; set; }
        [MaxLength(100)]
        public string giong_pet {  get; set; } = "";
        [MaxLength(100)]
        public string cannang_pet {  get; set; } = "";

        public int id_kh { get; set; }
    }
}
