using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using TechStoreWeb.Extensions;
using TechStoreWeb.Models;
using TechStoreWeb.Data;

namespace TechStoreWeb.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private const string SESSION_CART = "Cart";
        private const string SESSION_DISCOUNT = "CartDiscount";
        private const decimal SHIPPING_FEE = 20000m;

        private const decimal INSURANCE_RATE = 0.005m;
        private const decimal MIN_INSURANCE_FEE = 2000m;

        private static decimal CalculateInsuranceFee(decimal subtotal)
        {
            if (subtotal <= 0) return 0m;
            var fee = Math.Ceiling(subtotal * INSURANCE_RATE / 1000m) * 1000m;
            return Math.Max(fee, MIN_INSURANCE_FEE);
        }

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = GetCart();

            RefreshPrices(cart);
            SaveCart(cart);

            ViewBag.Discount = HttpContext.Session.GetObject<decimal>(SESSION_DISCOUNT);
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int productId, int? variantId, int qty = 1, string? action = null)
        {
            var isBuyNow = string.Equals(action, "buy", StringComparison.OrdinalIgnoreCase);

            if (qty < 1) qty = 1;

            var product = _context.Products.Find(productId);
            if (product == null)
            {
                return FailAdd("Sản phẩm không tồn tại.");
            }

            ProductVariant? variant = null;
            if (variantId.HasValue)
            {
                variant = _context.ProductVariants.FirstOrDefault(v => v.VariantId == variantId.Value && v.ProductId == productId);
                if (variant == null)
                {
                    return FailAdd("Phiên bản sản phẩm không hợp lệ.");
                }
            }

            var price = variant?.Price ?? product.Price;
            var name = variant == null ? product.Name : $"{product.Name} - {variant.Color} {variant.ROM}";
            var availableStock = variant?.Stock ?? product.Stock;

            var cart = GetCart();
            var cartId = CartItem.BuildId(productId, variantId);
            var existing = cart.FirstOrDefault(c => c.Id == cartId);
            var requestedTotal = (existing?.Qty ?? 0) + qty;

            if (availableStock <= 0)
            {
                return FailAdd($"\"{name}\" đã hết hàng.");
            }

            if (requestedTotal > availableStock)
            {
                return FailAdd($"\"{name}\" chỉ còn {availableStock} sản phẩm.");
            }

            if (isBuyNow)
            {
                foreach (var item in cart)
                {
                    item.Selected = false;
                }
            }

            if (existing != null)
            {
                existing.Qty = requestedTotal;
                existing.Price = price;
                existing.Name = name;
                existing.Selected = true;
            }
            else
            {
                cart.Add(new CartItem
                {
                    Id = cartId,
                    ProductId = productId,
                    VariantId = variantId,
                    Name = name,
                    Price = price,
                    Img = product.ImageUrl,
                    Qty = qty,
                    Selected = true
                });
            }

            SaveCart(cart);

            if (IsAjax())
            {
                return Json(new
                {
                    success = true,
                    message = isBuyNow ? "Đang chuyển tới trang thanh toán" : "Đã thêm vào giỏ hàng",
                    redirectUrl = isBuyNow ? Url.Action("Checkout") : null
                });
            }

            if (isBuyNow)
            {
                return RedirectToAction("Checkout");
            }

            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQty(string id, int qty)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.Id == id);
            if (item != null)
            {
                qty = Math.Max(1, qty);
                var availableStock = GetAvailableStock(item);

                if (qty > availableStock)
                {
                    qty = Math.Max(1, availableStock);
                    TempData["ErrorMessage"] = $"\"{item.Name}\" chỉ còn {availableStock} sản phẩm.";
                }

                item.Qty = qty;
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(string id)
        {
            var cart = GetCart().Where(c => c.Id != id).ToList();
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleSelect(string id, bool selected)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.Id == id);
            if (item != null)
            {
                item.Selected = selected;
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyVoucher(string code)
        {
            decimal discount = 0;
            if (!string.IsNullOrEmpty(code) && (code.ToUpper() == "CHUACO" || code == "Chưa có"))
            {
                discount = 100000m;
            }
            HttpContext.Session.SetObject(SESSION_DISCOUNT, discount);
            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });

            var cart = GetCart();
            RefreshPrices(cart);
            SaveCart(cart);

            var selectedItems = cart.Where(c => c.Selected).ToList();
            if (!selectedItems.Any()) return RedirectToAction("Index");

            ViewBag.User = _context.Users.Find(userId);
            ViewBag.Discount = HttpContext.Session.GetObject<decimal>(SESSION_DISCOUNT);
            ViewBag.ShippingFee = SHIPPING_FEE;
            ViewBag.InsuranceFee = CalculateInsuranceFee(selectedItems.Sum(i => i.Price * i.Qty));

            if (TempData["CheckoutForm"] is string savedForm && !string.IsNullOrEmpty(savedForm))
            {
                ViewBag.SavedForm = System.Text.Json.JsonSerializer.Deserialize<CheckoutFormModel>(savedForm);
            }

            return View(selectedItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(CheckoutFormModel form)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });

            var validationError = ValidateCheckoutForm(form);
            if (validationError != null)
            {
                TempData["ErrorMessage"] = validationError;
                TempData["CheckoutForm"] = System.Text.Json.JsonSerializer.Serialize(form);
                return RedirectToAction("Checkout");
            }

            var shippingAddress = BuildShippingAddress(form);
            var paymentMethod = string.IsNullOrWhiteSpace(form.PaymentMethod) ? "COD" : form.PaymentMethod;
            var shippingFee = SHIPPING_FEE;

            var cart = GetCart();
            var selectedItems = cart.Where(c => c.Selected).ToList();
            if (!selectedItems.Any()) return RedirectToAction("Index");

            using var transaction = _context.Database.BeginTransaction();

            var orderDetails = new List<OrderDetail>();
            decimal subtotal = 0;

            foreach (var item in selectedItems)
            {
                var product = _context.Products.Find(item.ProductId);
                if (product == null)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = $"Sản phẩm \"{item.Name}\" không còn tồn tại.";
                    return RedirectToAction("Index");
                }

                ProductVariant? variant = null;
                if (item.VariantId.HasValue)
                {
                    variant = _context.ProductVariants.FirstOrDefault(v => v.VariantId == item.VariantId.Value);
                    if (variant == null)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = $"Phiên bản \"{item.Name}\" không còn tồn tại.";
                        return RedirectToAction("Index");
                    }
                }

                var unitPrice = variant?.Price ?? product.Price;
                var availableStock = variant?.Stock ?? product.Stock;

                if (item.Qty > availableStock)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = availableStock <= 0
                        ? $"\"{item.Name}\" đã hết hàng."
                        : $"\"{item.Name}\" chỉ còn {availableStock} sản phẩm, vui lòng giảm số lượng.";
                    return RedirectToAction("Index");
                }

                var siblings = _context.ProductVariants
                    .Where(v => v.ProductId == product.ProductId)
                    .ToList();

                if (variant != null)
                {
                    variant.Stock -= item.Qty;
                    product.Stock = siblings.Sum(v => v.Stock);
                }
                else if (siblings.Count > 0)
                {
                    var remaining = item.Qty;
                    foreach (var sibling in siblings.Where(v => v.Stock > 0).OrderByDescending(v => v.Stock))
                    {
                        if (remaining <= 0) break;
                        var take = Math.Min(sibling.Stock, remaining);
                        sibling.Stock -= take;
                        remaining -= take;
                    }
                    product.Stock = siblings.Sum(v => v.Stock);
                }
                else
                {
                    product.Stock -= item.Qty;
                }

                subtotal += unitPrice * item.Qty;
                orderDetails.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Qty,
                    UnitPrice = unitPrice
                });
            }

            var discount = HttpContext.Session.GetObject<decimal>(SESSION_DISCOUNT);
            var insuranceFee = form.BuyInsurance ? CalculateInsuranceFee(subtotal) : 0m;
            var total = Math.Max(subtotal + shippingFee + insuranceFee - discount, 0m);

            var order = new Order
            {
                UserId = userId.Value,
                OrderDate = DateTime.Now,
                TotalAmount = total,
                Status = "Pending",
                PaymentMethod = paymentMethod,
                ShippingAddress = shippingAddress,
                ReceiverName = form.ReceiverName?.Trim(),
                ReceiverPhone = form.ReceiverPhone?.Trim(),
                ReceiverEmail = form.ReceiverEmail?.Trim(),
                Note = string.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim(),
                ShippingMethod = "Standard",
                ShippingFee = shippingFee,
                InsuranceFee = insuranceFee,
                OrderDetails = orderDetails
            };

            _context.Orders.Add(order);
            _context.SaveChanges();
            transaction.Commit();

            return RedirectToAction("Payment", new { id = order.OrderId });
        }

        public IActionResult Payment(int id)
        {
            var order = GetOwnedOrder(id);
            if (order == null) return RedirectToAction("Index", "Home");

            if (order.PaymentMethod == "COD")
            {
                return RedirectToAction("Confirm", new { id = order.OrderId });
            }

            return View(order);
        }

        public IActionResult Confirm(int id)
        {
            var order = GetOwnedOrder(id);
            if (order == null) return RedirectToAction("Index", "Home");

            var cart = GetCart();
            if (cart.Any(c => c.Selected))
            {
                SaveCart(cart.Where(c => !c.Selected).ToList());
                HttpContext.Session.Remove(SESSION_DISCOUNT);
            }

            return View(order);
        }


        private static string? ValidateCheckoutForm(CheckoutFormModel form)
        {
            if (string.IsNullOrWhiteSpace(form.ReceiverName) || form.ReceiverName.Trim().Length < 2)
                return "Vui lòng nhập họ tên người nhận.";

            var phone = (form.ReceiverPhone ?? string.Empty).Replace(" ", "").Replace(".", "").Replace("-", "");
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(0|\+84)([0-9]{9})$"))
                return "Số điện thoại không hợp lệ. Vui lòng nhập số di động 10 chữ số.";

            if (!string.IsNullOrWhiteSpace(form.ReceiverEmail)
                && !System.Text.RegularExpressions.Regex.IsMatch(form.ReceiverEmail.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "Email không hợp lệ.";

            if (string.IsNullOrWhiteSpace(form.Province))
                return "Vui lòng chọn Tỉnh/Thành phố.";

            if (string.IsNullOrWhiteSpace(form.Ward))
                return "Vui lòng nhập Phường/Xã.";

            if (string.IsNullOrWhiteSpace(form.AddressDetail) || form.AddressDetail.Trim().Length < 5)
                return "Vui lòng nhập địa chỉ chi tiết (số nhà, tên đường).";

            return null;
        }

        private static string BuildShippingAddress(CheckoutFormModel form)
        {
            var parts = new List<string>();

            var detail = form.AddressDetail?.Trim();
            if (!string.IsNullOrWhiteSpace(form.AddressType))
            {
                var label = string.Equals(form.AddressType, "Office", StringComparison.OrdinalIgnoreCase)
                    ? "Văn phòng"
                    : "Nhà riêng";
                detail = $"{detail} ({label})";
            }

            parts.Add(detail!);
            if (!string.IsNullOrWhiteSpace(form.Ward)) parts.Add(form.Ward.Trim());
            if (!string.IsNullOrWhiteSpace(form.District)) parts.Add(form.District.Trim());
            if (!string.IsNullOrWhiteSpace(form.Province)) parts.Add(form.Province.Trim());

            return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }


        private Order? GetOwnedOrder(int orderId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return null;

            return _context.Orders.FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId.Value);
        }

        private List<CartItem> GetCart()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(SESSION_CART) ?? new List<CartItem>();

            return cart.Where(c => c.ProductId > 0).ToList();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObject(SESSION_CART, cart);
        }

        private int GetAvailableStock(CartItem item)
        {
            if (item.VariantId.HasValue)
            {
                return _context.ProductVariants
                    .Where(v => v.VariantId == item.VariantId.Value)
                    .Select(v => (int?)v.Stock)
                    .FirstOrDefault() ?? 0;
            }

            return _context.Products
                .Where(p => p.ProductId == item.ProductId)
                .Select(p => (int?)p.Stock)
                .FirstOrDefault() ?? 0;
        }

        private void RefreshPrices(List<CartItem> cart)
        {
            foreach (var item in cart)
            {
                var product = _context.Products.Find(item.ProductId);
                if (product == null) continue;

                if (item.VariantId.HasValue)
                {
                    var variant = _context.ProductVariants.FirstOrDefault(v => v.VariantId == item.VariantId.Value);
                    if (variant == null) continue;

                    item.Price = variant.Price ?? product.Price;
                    item.Name = $"{product.Name} - {variant.Color} {variant.ROM}";
                }
                else
                {
                    item.Price = product.Price;
                    item.Name = product.Name;
                }

                item.Img = product.ImageUrl;
            }
        }

        private bool IsAjax()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        private IActionResult FailAdd(string message)
        {
            if (IsAjax())
            {
                return Json(new { success = false, message });
            }

            TempData["ErrorMessage"] = message;
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }
    }
}
