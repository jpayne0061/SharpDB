﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SharpDbConsole
{
    public class House
    {
        public int NumBedrooms { get; set; }
        public int NumBath { get; set; }
        public decimal Price { get; set; }
        public bool IsListed { get; set; }

        [StringLength(50)]
        public string Address { get; set; }
    }
}
