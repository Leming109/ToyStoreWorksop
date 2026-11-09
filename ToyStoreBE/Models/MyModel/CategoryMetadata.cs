using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ToyStoreBE.Models.MyModel
{
    [MetadataType(typeof(CategoryMetadata))]
    public partial class Category
    {
    }
    public class CategoryMetadata
    {
        [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
        [StringLength(100,
            ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; }
    }
}