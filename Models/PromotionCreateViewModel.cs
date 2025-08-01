// Models/PromotionCreateViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ServiPuntosUyAdmin.Models
{
    public class PromotionCreateViewModel
    {
        // Inyectados desde sesión
        public int BranchId { get; set; }
        public int TenantId { get; set; }

        [Required]
        [Display(Name = "Descripción")]
        public string Description { get; set; } = "";

        [Required]
        [Display(Name = "Fecha inicio")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Fecha fin")]
        [DataType(DataType.DateTime)]
        public DateTime EndDate { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Precio")]
        public decimal Price { get; set; }

        // Aquí las colecciones que faltaban:
        [Display(Name = "Sucursales")]
        public List<int> Branches { get; set; } = new();

        [Required(ErrorMessage = "Debe seleccionar al menos un producto")]
        [Display(Name = "Productos")]
        public List<int> Products { get; set; } = new();

         // Lista de productos para el dropdown
        public List<SelectListItem> AvailableProducts { get; set; } = new();
    }
}
