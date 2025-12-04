using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnowledgeSpace.ViewModels.Contents;
using KnowledgeSpace.WebPortal.Extensions;
using KnowledgeSpace.WebPortal.Helpers;
using KnowledgeSpace.WebPortal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KnowledgeSpace.WebPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserApiClient _userApiClient;
        private readonly IKnowledgeBaseApiClient _knowledgeBaseApiClient;
        private readonly ICategoryApiClient _categoryApiClient;

        public AccountController(IUserApiClient userApiClient,
            IKnowledgeBaseApiClient knowledgeBaseApiClient,
            ICategoryApiClient categoryApiClient)
        {
            _userApiClient = userApiClient;
            _categoryApiClient = categoryApiClient;
            _knowledgeBaseApiClient = knowledgeBaseApiClient;
        }

        public IActionResult SignIn()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = "/" }, "oidc");
        }

        public IActionResult SignOut()
        {
            return SignOut(new AuthenticationProperties { RedirectUri = "/" }, "Cookies", "oidc");
        }

        [Authorize]
        public async Task<ActionResult> MyProfile()
        {
            var user = await _userApiClient.GetById(User.GetUserId());
            return View(user);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyKnowledgeBases(int page = 1, int pageSize = 10)
        {
            var kbs = await _userApiClient.GetKnowledgeBasesByUserId(User.GetUserId(), page, pageSize);
            return View(kbs);
        }

        [HttpGet]
        public async Task<IActionResult> CreateNewKnowledgeBase()
        {
            try
            {
                // DEBUG: Xem API trả về gì
                var categories = await _categoryApiClient.GetCategories();

                ViewBag.RawCategories = categories != null ?
                    $"Count: {categories.Count} - Data: {string.Join(", ", categories.Select(c => $"[{c.Id}:{c.Name}]"))}" :
                    "NULL";

                await SetCategoriesViewBag();

                var cats = ViewBag.Categories as List<SelectListItem>;
                if (cats != null)
                {
                    ViewBag.DebugInfo = $"Có {cats.Count} items trong ViewBag";
                    ViewBag.DetailInfo = string.Join(" | ", cats.Select(c => $"{c.Value}:{c.Text}"));
                }
                else
                {
                    ViewBag.DebugInfo = "ViewBag.Categories = NULL";
                    ViewBag.DetailInfo = "N/A";
                }
            }
            catch (Exception ex)
            {
                ViewBag.DebugInfo = $"LỖI: {ex.Message}";
                ViewBag.RawCategories = $"Exception: {ex.ToString()}";
                ViewBag.DetailInfo = "Error occurred";
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewKnowledgeBase([FromForm] KnowledgeBaseCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!Captcha.ValidateCaptchaCode(request.CaptchaCode, HttpContext))
            {
                ModelState.AddModelError("", "Mã xác nhận không đúng");
                return BadRequest(ModelState);
            }

            var result = await _knowledgeBaseApiClient.PostKnowlegdeBase(request);
            if (result)
            {
                return Ok();
            }
            return BadRequest();
        }

        [HttpGet]
        public async Task<IActionResult> EditKnowledgeBase(int id)
        {
            var knowledgeBase = await _knowledgeBaseApiClient.GetKnowledgeBaseDetail(id);
            await SetCategoriesViewBag();
            return View(new KnowledgeBaseCreateRequest()
            {
                CategoryId = knowledgeBase.CategoryId,
                Description = knowledgeBase.Description,
                Environment = knowledgeBase.Environment,
                ErrorMessage = knowledgeBase.ErrorMessage,
                Labels = knowledgeBase.Labels,
                Note = knowledgeBase.Note,
                Problem = knowledgeBase.Problem,
                StepToReproduce = knowledgeBase.StepToReproduce,
                Title = knowledgeBase.Title,
                Workaround = knowledgeBase.Workaround,
                Id = knowledgeBase.Id
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditKnowledgeBase([FromForm] KnowledgeBaseCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (!Captcha.ValidateCaptchaCode(request.CaptchaCode, HttpContext))
            {
                ModelState.AddModelError("", "Mã xác nhận không đúng");
                return BadRequest(ModelState);
            }

            var result = await _knowledgeBaseApiClient.PutKnowlegdeBase(request.Id.Value, request);
            if (result)
            {
                return Ok();
            }
            return BadRequest();
        }

        private async Task SetCategoriesViewBag(int? selectedValue = null)
        {
            var categories = await _categoryApiClient.GetCategories();

            // Tạo list items
            var items = new List<SelectListItem>();

            // Thêm option mặc định
            items.Add(new SelectListItem
            {
                Value = "",
                Text = "--Chọn danh mục--",
                Selected = !selectedValue.HasValue
            });

            // Kiểm tra categories có dữ liệu không
            if (categories != null && categories.Any())
            {
                foreach (var category in categories)
                {
                    items.Add(new SelectListItem
                    {
                        Text = category.Name,
                        Value = category.Id.ToString(),
                        Selected = selectedValue.HasValue && category.Id == selectedValue.Value
                    });
                }
            }

            ViewBag.Categories = items;
        }
    }
}