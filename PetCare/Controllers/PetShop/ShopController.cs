﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PetCare.Models;
using PetCare.Models.Authentication;
using PetCare.Services;

namespace PetCare.Controllers.PetShop
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext context;
        int? idKhachHang;
        public ShopController(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task<IActionResult> Index(SanphamDto spdto)
        {
            int? userId = HttpContext.Session.GetInt32("Username");
            if (userId == null)
            {
                HttpContext.Session.Clear();
            }
            var user = await context.Khachhangs.FindAsync(userId);

            ViewData["UserId"] = user?.id_kh;
            ViewData["UserName"] = user?.ten_kh;
            var sanpham = context.Sanphams.ToList();
            return View(sanpham);
        }
        public IActionResult Category()
        {

            return View();
        }

        public IActionResult Product()
        {

            return View();
        }
        [HttpGet]
        [Route("/Checkout")]
        public async Task<IActionResult> CheckOut(int id)
        {
            // Fetch the cart items from the session
            var cart = GetCartItems();
            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            int? userId = HttpContext.Session.GetInt32("Username");
            idKhachHang = userId;
            if (userId == null)
            {
                HttpContext.Session.Clear();
            }
            var user = await context.Khachhangs.FindAsync(userId);

            // Populate the ViewModel
            var model = new VMThongtinthanhtoan
            {
                ten_kh = user.ten_kh,
                sdt_kh = user.sdt_kh,
                giohangs = cart // Add the cart items to the ViewModel
            };

            // Pass the model to the view
            return View(model);
        }
        private string GenerateOrderCode()
        {
            return $"{DateTime.Now:yyyyMMddHHmmss}";
        }

        [HttpPost]
        [Route("/Checkout")]
        public IActionResult CheckOut(VMThongtinthanhtoan vm)
        {
            var cart = GetCartItems();
            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            // Calculate total price
            decimal totalPrice = cart.Sum(item => item.Sanpham.thanhtien * item.soluong);

            int? userId = HttpContext.Session.GetInt32("Username");
            // Create a new order
            var order = new Donhang
            {
                ma_dh = GenerateOrderCode(),
                id_kh = (int)idKhachHang,
                diachi_giao = vm.diachi_giao,
                ghi_chu = vm.ghi_chu,
                tong_tien = totalPrice,
                trang_thai = "Đã tạo đơn",
                CreateAt = DateTime.Now
            };
            context.Donhangs.Add(order);
            context.SaveChanges();

            // Add order details
            foreach (var item in cart)
            {
                var orderDetail = new Chitietdon
                {
                    id_dh = order.id_dh,
                    id_sp = item.Sanpham.id_sanpham,
                    soluong = item.soluong
                };
                context.Chitietdons.Add(orderDetail);
            }
            context.SaveChanges();

            // Clear cart
            ClearCart();

            TempData["Success"] = "Order placed successfully!";
            return RedirectToAction("Cart");
        }

        /// Thêm sản phẩm vào cart
        [Route("addcart/{productid:int}", Name = "addcart")]
        public IActionResult AddToCart([FromRoute] int productid)
        {

            var product = context.Sanphams
                .Where(p => p.id_sanpham == productid)
                .FirstOrDefault();
            if (product == null)
                return NotFound("Không có sản phẩm");

            // Xử lý đưa vào Cart ...
            var cart = GetCartItems();
            var cartitem = cart.Find(p => p.Sanpham.id_sanpham == productid);
            if (cartitem != null)
            {
                // Đã tồn tại, tăng thêm 1
                cartitem.soluong++;
            }
            else
            {
                //  Thêm mới
                cart.Add(new Giohang() { soluong = 1, Sanpham = product });
            }

            // Lưu cart vào Session
            SaveCartSession(cart);
            // Chuyển đến trang hiện thị Cart
            return RedirectToAction(nameof(Cart));
        }
        /// xóa item trong cart
        [Route("/removecart/{productid:int}", Name = "removecart")]
        public IActionResult RemoveCart([FromRoute] int productid)
        {
            var cart = GetCartItems();
            var cartitem = cart.Find(p => p.Sanpham.id_sanpham == productid);
            if (cartitem != null)
            {
                // Đã tồn tại, tăng thêm 1
                cart.Remove(cartitem);
            }

            SaveCartSession(cart);
            return RedirectToAction(nameof(Cart));
        }

        /// Cập nhật
        [Route("/updatecart", Name = "updatecart")]
        [HttpPost]
        public IActionResult UpdateCart([FromForm] int productid, [FromForm] int quantity)
        {
            // Cập nhật Cart thay đổi số lượng quantity ...
            var cart = GetCartItems();
            var cartitem = cart.Find(p => p.Sanpham.id_sanpham == productid);
            if (cartitem != null)
            {
                // Đã tồn tại, tăng thêm 1
                cartitem.soluong = quantity;
            }
            SaveCartSession(cart);
            // Trả về mã thành công (không có nội dung gì - chỉ để Ajax gọi)
            return Ok();
        }


        // Hiện thị giỏ hàng
        [Authentication]
        [Route("/Cart", Name = "Cart")]
        public IActionResult Cart()
        {
            return View(GetCartItems());
        }

        public const string CARTKEY = "cart";

        // Lấy cart từ Session (danh sách CartItem)
        List<Giohang> GetCartItems()
        {
            string? jsoncart = HttpContext.Session.GetString(CARTKEY);
            if (jsoncart != null)
            {
                return JsonConvert.DeserializeObject<List<Giohang>>(jsoncart);
            }
            return new List<Giohang>();
        }

        // Xóa cart khỏi session
        void ClearCart()
        {
            HttpContext.Session.Remove(CARTKEY);
        }

        // Lưu Cart (Danh sách CartItem) vào session
        void SaveCartSession(List<Giohang> ls)
        {
            string jsoncart = JsonConvert.SerializeObject(ls);
            HttpContext.Session.SetString(CARTKEY, jsoncart);
        }
    }
}
