using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ToyStoreBE.Models.MyModel
{
    public class UserMetadata
    {
        [MetadataType(typeof(UserMetadata))]
        public partial class User
        {
        }

        
            [Required(ErrorMessage = "Vui lòng nhập tên tài khoản.")]
            [StringLength(255, ErrorMessage = "Tên tài khoản tối đa 255 ký tự.")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            [StringLength(50, ErrorMessage = "Mật khẩu tối đa 50 ký tự.")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
            [RegularExpression("^(A|C)$", ErrorMessage = "Vai trò chỉ được là A hoặc C.")]
            public string UserRole { get; set; }
    }
}