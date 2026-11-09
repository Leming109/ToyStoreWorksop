using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ToyStoreBE.Models.MyModel
{
    public class AdminUserViewModels
    {
        public string Username { get; set; }
        public string UserRole { get; set; }
    }

    public class AdminUserCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên tài khoản.")]
        [StringLength(255, MinimumLength = 3,
            ErrorMessage = "Tên tài khoản phải từ 3 đến 255 ký tự.")]
        [RegularExpression(@"^[A-Za-z0-9._@-]+$",
            ErrorMessage = "Tên tài khoản chỉ được chứa chữ, số và các ký tự . _ @ -.")]
        [Display(Name = "Tên tài khoản")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(50, MinimumLength = 8,
            ErrorMessage = "Mật khẩu phải từ 8 đến 50 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare("Password",
            ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }
    }

    public class AdminUserEditViewModel : IValidatableObject
    {
        public string Username { get; set; }

        [StringLength(50, MinimumLength = 8,
            ErrorMessage = "Mật khẩu mới phải từ 8 đến 50 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmNewPassword { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            // Không nhập mật khẩu mới thì giữ nguyên mật khẩu cũ.
            if (string.IsNullOrWhiteSpace(NewPassword))
                yield break;

            if (string.IsNullOrWhiteSpace(ConfirmNewPassword))
            {
                yield return new ValidationResult(
                    "Vui lòng xác nhận mật khẩu mới.",
                    new[] { "ConfirmNewPassword" });
            }
            else if (NewPassword != ConfirmNewPassword)
            {
                yield return new ValidationResult(
                    "Mật khẩu xác nhận không khớp.",
                    new[] { "ConfirmNewPassword" });
            }
        }
    }
}