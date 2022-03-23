﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ECommerceLiteBLL.Account;
using ECommerceLiteBLL.Repository;
using ECommerceLiteBLL.Settings;
using ECommerceLiteEntity.Enums;
using ECommerceLiteEntity.IdentityModels;
using ECommerceLiteEntity.Models;
using ECommerceLiteUI.Models;
using ECommerceLiteEntity.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;

namespace ECommerceLiteUI.Controllers
{
    public class AccountController : BaseController
    {
        //Global alan
        CustomerRepo myCustomerRepo = new CustomerRepo();
        PassiveUserRepo myPassiveUserRepo =
    new PassiveUserRepo();
        UserManager<ApplicationUser> myUsermanager = MembershipTools.NewUserManager();
        UserStore<ApplicationUser> myUserStore = MembershipTools.NewUserStore();
        RoleManager<ApplicationRole> myRoleManager = MembershipTools.NewRoleManager();

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                var checkUserTC = myUserStore.Context.Set<Customer>()
                    .FirstOrDefault(x => x.TCNumber == model.TCNumber)?.TCNumber;
                if (checkUserTC != null)
                {
                    ModelState.AddModelError("", "Bu TC numarası ile daha önceden sisteme kayıt olunmuştur!");
                    return View(model);
                }

                var checkUserEmail = myUserStore.Context.Set<ApplicationUser>()
                  .FirstOrDefault(x => x.Email == model.Email)?.Email;
                if (checkUserEmail != null)
                {
                    ModelState.AddModelError("", "Bu email adresi sisteme zaten kayıtlıdır. Şifrenizi unuttuysanız Şifremi unuttum ile yeni şifre alabilirsiniz!");
                    return View(model);
                }

                var theActivationCode =
                    Guid.NewGuid().ToString().Replace("-", "");
                var newUser = new ApplicationUser()
                {
                    Name = model.Name,
                    Surname = model.Surname,
                    Email = model.Email,
                    ActivationCode = theActivationCode,
                    UserName = model.Email,
                    //TODO: yarın düzenlenecek
                    PhoneNumber = "05396796650"
                };
                var theResult = myUsermanager.CreateAsync(newUser, model.Password);
                if (theResult.Result.Succeeded)
                {
                    //AspnetUsers tablosuna kayıt gerçekleşirse
                    //yeni kayıt olmuş bu kişiyi pasif tablosuna ekleyeceğiz
                    // Kişi kendisine gelen aktifleşme işlemini yaparsa
                    //PasifKullanıcılar tablosundan kendisini silip olması gereken roldeki tabloya ekleyeceğiz.
                    await myUsermanager.AddToRoleAsync(newUser.Id, TheIdentityRoles.Passive.ToString());
                    PassiveUser newPassiveUser = new PassiveUser()
                    {
                        TCNumber = model.TCNumber,
                        UserId = newUser.Id,
                        TargetRole = TheIdentityRoles.Customer,
                        LastActiveTime = DateTime.Now
                    };
                    myPassiveUserRepo.Insert(newPassiveUser);
                    string siteUrl =
                         Request.Url.Scheme + Uri.SchemeDelimiter
                         + Request.Url.Host
                         + (Request.Url.IsDefaultPort ? "" : ":" + Request.Url.Port);

                    await SiteSettings.SendMail(new MailModel()
                    {
                        To = newUser.Email,
                        Subject = "ECommerceLite Site Aktivasyon",
                        Message = $"Merhaba {newUser.Name} {newUser.Surname}, <br/>Hesabınızı aktifleştirmek için <b><a href='{siteUrl}/Account/Activation?code={theActivationCode}'>Aktivasyon Linkine</a></b> tıklayınız..."
                    });
                    return RedirectToAction
                        ("Login", "Account"
                        , new { email = $"{newUser.Email}" });

                }
                else
                {
                    ModelState.AddModelError("", "Kullanıcı kayıt işleminde hata oluştu!");
                    return View(model);
                }


            }
            catch (Exception ex)
            {
                //TODO: ex Loglama
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu!");
                return View(model);
            }

        }


        [HttpGet]
        public async Task<ActionResult> Activation(string code)
        {
            try
            {
                var theUser = myUserStore.Context.Set<ApplicationUser>().FirstOrDefault(x => x.ActivationCode == code);
                if (theUser == null)
                {
                    ViewBag.ActivationResult = "Aktivasyon işlemi  başarısız";
                    return View();
                }

                if (theUser.EmailConfirmed)
                {
                    ViewBag.ActivationResult = "E-Posta adresiniz zaten onaylı";
                    return View();
                }
                theUser.EmailConfirmed = true;
                await myUserStore.UpdateAsync(theUser);
                await myUserStore.Context.SaveChangesAsync();
                //Kullanıcıyı passiveuser tablosundan bulalım
                PassiveUser thePassiveUser = myPassiveUserRepo.Queryable().FirstOrDefault(x => x.UserId == theUser.Id);
                if (thePassiveUser != null)
                {
                    if (thePassiveUser.TargetRole == TheIdentityRoles.Customer)
                    {
                        //yeni customer oluşacak ve kaydedilecek
                        Customer newCustomer = new Customer()
                        {
                            TCNumber = thePassiveUser.TCNumber,
                            UserId = theUser.Id,
                            LastActiveTime = DateTime.Now
                        };
                        myCustomerRepo.Insert(newCustomer);
                        //Pasif tablosundan bu kayıt silinsin
                        myPassiveUserRepo.Delete(thePassiveUser);
                        //userdaki passive rol silinip customer rol eklenecek
                        myUsermanager.RemoveFromRole(theUser.Id, TheIdentityRoles.Passive.ToString());
                        myUsermanager.AddToRole(theUser.Id, TheIdentityRoles.Customer.ToString());
                        ViewBag.ActivationResult = $"Merhaba {theUser.Name} {theUser.Surname}, aktivasyon işleminiz başarılıdır.";
                        return View();

                    }
                }
                return View();
            }
            catch (Exception ex)
            {

                //TODO: ex Loglama
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu!");
                return View();
            }


        }


        [HttpGet]
        public ActionResult Login(string ReturnUrl, string email)
        {
            try
            {
                if (HttpContext.User.Identity.IsAuthenticated 
                    && ReturnUrl != null)
                {
                    var url = ReturnUrl.Split('/');
                    //TODO:devam edebilir.
                }
                var model = new LoginViewModel()
                {
                    ReturnUrl = ReturnUrl
                };
                return View(model);
            }
            catch (Exception ex)
            {
                //ex loglanacak
                return View();

            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var theUser = await myUsermanager.FindAsync(model.Email, model.Password);
                if (theUser == null)
                {
                    ModelState.AddModelError(string.Empty, "Emailinizi veya şifrenizi doğru girdiğinizden emin olunuz!");
                    return View(model);
                }
                if (theUser.Roles.FirstOrDefault().RoleId == myRoleManager.FindByName(Enum.GetName(typeof(TheIdentityRoles), TheIdentityRoles.Passive)).Id)
                {
                    ViewBag.TheResult = "Sistemi kullanabilmeniz için üyeliğinizi aktifleştirmeniz gerekmektedir. Emailinize gönderilen aktivasyon linkine tıklayarak aktifleştirme işlemini yapabilirsiniz!";
                    return View(model);
                }
                var authManager = HttpContext.GetOwinContext().Authentication;
                var userIdentity = await myUsermanager.CreateIdentityAsync(theUser, DefaultAuthenticationTypes.ApplicationCookie);
                authManager.SignIn(new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe
                }, userIdentity);
                if (theUser.Roles.FirstOrDefault().RoleId == myRoleManager.FindByName(Enum.GetName(typeof(TheIdentityRoles), TheIdentityRoles.Admin)).Id)
                {
                    return RedirectToAction("Dashboard", "Admin");

                }
                if (theUser.Roles.FirstOrDefault().RoleId == myRoleManager.FindByName(Enum.GetName(typeof(TheIdentityRoles), TheIdentityRoles.Customer)).Id)
                {
                    return RedirectToAction("Index", "Home");

                }

                if (string.IsNullOrEmpty(model.ReturnUrl))
                    return RedirectToAction("Index", "Home");

                var url = model.ReturnUrl.Split('/');
                if (url.Length == 4)
                {
                    return RedirectToAction(url[2], url[1], new { id = url[3] });
                }
                else
                {
                    return RedirectToAction(url[2], url[1]);
                }
            }
            catch (Exception ex)
            {
                //TODO ex loglanacak
                ModelState.AddModelError("", "Beklenmedik hata oluştu!");
                return View(model);

            }
        }

        [HttpGet]
        public ActionResult UpdatePassword()
        {
            var theUser = myUsermanager
                .FindById(HttpContext.User.Identity.GetUserId());
            if (theUser!=null)
            {
                ProfileViewModel model = new ProfileViewModel()
                {
                    Name=theUser.Name,
                    Surname=theUser.Surname,
                    Email=theUser.Email,
                    Username=theUser.UserName
                };
                return View(model);
            }

            return View();
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdatePassword(ProfileViewModel model)
        {
            try
            {
                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    ModelState.AddModelError("", "Şifreler uyuşmuyor!");
                    //TODO: Profile göndermişiz ???
                    return View(model);
                }
                var theUser = myUsermanager
                    .FindById(HttpContext.User.Identity.GetUserId());
                var theCheckUser = myUsermanager
                    .Find(theUser.UserName, model.CurrentPassword);
                if (theCheckUser == null)
                {
                    ModelState.AddModelError("", "Mevcut şifrenizi yanlış girdiniz!");
                    //TODO: Profile göndermişiz ???
                    return View();
                }

                await myUserStore.SetPasswordHashAsync(theUser,
                    myUsermanager.PasswordHasher.HashPassword(model.NewPassword));
               await myUsermanager.UpdateAsync(theUser);
                TempData["PasswordUpdated"] = "Şifreniz değiştirilmiştir!";
                HttpContext.GetOwinContext()
                    .Authentication.SignOut();
                return RedirectToAction("Login", "Account", new { email = theUser.Email });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu!");
                return View(model);
                //TODO ex loglanacak
            }
        }


        [Authorize]
        [HttpGet]
        public ActionResult UserProfile()
        {
            var theUser = myUsermanager.FindById(HttpContext.User.Identity.GetUserId());

            var model = new ProfileViewModel()
            {
                Email = theUser.Email,
                Name = theUser.Name,
                Surname = theUser.Surname,
                Username = theUser.UserName
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserProfile(ProfileViewModel model)
        {
            try
            {
                var theUser = myUsermanager
                    .FindById(HttpContext.User.Identity.GetUserId());
                if (theUser == null)
                {
                    ModelState.AddModelError("", "Kullanıcı bulunamadığı için işlem yapılamıyor!");
                    return View(model);
                }
                if (myUsermanager.PasswordHasher.VerifyHashedPassword(theUser.PasswordHash, model.CurrentPassword)
                    == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("", "Mevcut şifrenizi yanlış girdiğiniz için bilgilerinizi güncelleyemiyoruz!");
                    return View(model);
                }

                theUser.Name = model.Name;
                theUser.Surname = model.Surname;
                //TODO: telefon numarası eklenebilir.
                await myUsermanager.UpdateAsync(theUser);
                ViewBag.TheResult = "Bilgileriniz güncelleşmiştir";
                var newModel = new ProfileViewModel()
                {
                    Email = theUser.Email,
                    Name = theUser.Name,
                    Surname = theUser.Surname,
                    Username = theUser.UserName
                };
                return View(newModel);

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu! HATA:" + ex.Message);
                return View(model);
                //TODO: ex loglanacak
            }
        }

        [HttpGet]
        public ActionResult RecoverPassword()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RecoverPassword(ProfileViewModel model)
        {
            try
            {
                var theUser =
                    myUserStore.Context.Set<ApplicationUser>()
                    .FirstOrDefault(x => x.Email == model.Email);
                if (theUser == null)
                {
                    ViewBag.TheResult = "Sistemde böyle bir kullanıcı olmadığı için şifre yenileyemiyoruz. Lütfen önce sisteme kayıt olunuz!";
                    return View(model);
                }
                var randomPassword = CreateRandomNewPassword();
                await myUserStore.SetPasswordHashAsync(theUser, myUsermanager.PasswordHasher.HashPassword(randomPassword));
                await myUserStore.UpdateAsync(theUser);
                string siteUrl = Request.Url.Scheme + Uri.SchemeDelimiter + Request.Url.Host +
                    (Request.Url.IsDefaultPort ? "" : ":" + Request.Url.Port);
                await SiteSettings.SendMail(new MailModel()
                {
                    To = theUser.Email,
                    Subject = "ECommerceLite Site - Şifreniz Yenilendi",
                    Message = $"Merhaba {theUser.Name} {theUser.Surname} <br/>Yeni Şifreniz :<b>{randomPassword}</b><br/>" +
                    $"Sisteme giriş yapmak için <b><a href='{siteUrl}/Account/Login?email={theUser.Email}'>BURAYA</a></b> tıklayınız."
                });
                ViewBag.TheResult = "Email adresinize yeni şifreniz gönderilmiştir";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.TheResult = "Sistemsel bir hata oluştu! Tekrar deneyiniz!";
                return View(model);
                //TODO: ex loglanacak
            }
        }

        [Authorize]
        public ActionResult Logout()
        {
            Session.Clear();
            HttpContext.GetOwinContext().Authentication.SignOut();
            return RedirectToAction("Login", "Account");
        }
    }
}