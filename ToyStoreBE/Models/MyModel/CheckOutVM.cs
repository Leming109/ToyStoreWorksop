using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using ToyStoreBE.Models.MyModel;

namespace ToyStoreBE.Models.MyModel
{
    public class CheckOutVM
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(15)]
        public string CustomerPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string CustomerEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        public string CustomerAddress { get; set; }

        public List<CartItem> CartItems { get; set; }

        public decimal TotalAmount
        {
            get
            {
                return CartItems == null
                    ? 0
                    : CartItems.Sum(x => x.UnitPrice * x.Quantity);
            }
        }

        public int TotalQuantity
        {
            get
            {
                return CartItems == null
                    ? 0
                    : CartItems.Sum(x => x.Quantity);
            }
        }
    }
}
