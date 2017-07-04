using CaptchaMvc.HtmlHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using WebApp.Hash;
using WebApp.Models;
using WebApp.Repository;
using WebApp.ViewModel;

namespace WebApp.Controllers
{

    public class PlayerController : Controller
    {

        PlayerDb db = new PlayerDb();
        List<Player> listPlayers = new List<Player>();

        public ActionResult GetPlayer()
        {
            db = new PlayerDb();

            List<Player> listPlayers = new List<Player>();
            listPlayers = db.GetPlayers();
            return View(listPlayers);
        }

        public ActionResult Create()
        {

            return View();
        }

        [HttpPost]
        public ActionResult Create(Player player)
        {
            db = new PlayerDb();
            List<Player> listPlayer = new List<Player>();


            if (ModelState.IsValid && this.IsCaptchaValid("Correct"))
            {
                try
                {

                    db.InsertPlayer(player);
                    WebMail.Send(player.Email, "Login Link", "http://localhost:60387/Player/Prijava");
                    TempData["novikorisnik"] = player.KorisnickoIme + " je uspješno registriran/a. ";
                    TempData["info"] = "Vaš korisnički račun je uspješno kreiran. Poslana je poruka za aktivaciju na vašu el. poštu";


                    return RedirectToAction("EmailConfirmation", listPlayer);

                }
                catch (Exception)
                {
                    ViewBag.ErrorMessage = "Greška: captcha nije validna.";

                    ModelState.AddModelError("", "Korisnik je već registriran. Korisničko ime ili el. pošta su zauzeti");
                    return View();
                }

            }
            else if (!this.IsCaptchaValid("is not valid"))
            {
                ModelState.AddModelError("", "Captcha not valid");
                return View(player);
            }
            else if (player.KorisnickoIme == player.Lozinka)
            {
                ModelState.AddModelError("", "");
                return View(player);
            }
            else if (player.Lozinka == player.Email)
            {
                ModelState.AddModelError("", "Lozinka mora biti različita od el. pošte");
                return View(player);
            }

            else if (player.Email != player.EmailPonovo)
            {
                ModelState.AddModelError("", "Pogrešno unesena el. pošta");
                return View(player);
            }

            else
            {
                ModelState.AddModelError("", "Neuspješno uneseni podaci!Player nije dodan u bazu");
                return View(player);
            }
        }

        public ActionResult ForgotPassword()
        {

            return View();
        }

        public ActionResult ForgotUserName()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgotUserName(UserNameViewModel player)
        {
            db = new PlayerDb();
            List<UserNameViewModel> listPlayer = new List<UserNameViewModel>();




            if (ModelState.IsValid && this.IsCaptchaValid("Correct"))
            {
                try
                {

                    db.ForgetUser(player);

                    TempData["zaboravljenpass"] = player.Email + " je poslan. ";


                    return RedirectToAction("EmailConfirmation", listPlayer);

                }
                catch (Exception)
                {
                    ViewBag.ErrorMessage = "Greška: captcha nije validna.";

                    ModelState.AddModelError("", "Korisnik je već registriran. Korisničko ime ili el. pošta su zauzeti");
                    return View();
                }

            }
            else if (!this.IsCaptchaValid("is not valid"))
            {
                ModelState.AddModelError("", "Captcha not valid");
                return View(player);
            }

            return View();
        }

        public ActionResult Index()
        {
            return View();
        }



        [AllowAnonymous]
        public ActionResult Login()
        {
            Player player = new Player();
            return View(player);
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(AccountViewModel playervm)
        {

            List<AccountViewModel> listPlayer = new List<AccountViewModel>();
            listPlayer = db.Validate();


            try
            {

                if (listPlayer.Any(s => s.KorisnickoIme == playervm.KorisnickoIme && HashedPassword.Confirm(playervm.Lozinka, s.Lozinka, HashedPassword.SupportedHashAlgorithms.SHA256))
                    && !MvcApplication.LoginCounter.ReturnIfLockedUsername(playervm.KorisnickoIme))
                {


                    //listPlayer.SingleOrDefault(s => s.KorisnickoIme == playervm.KorisnickoIme && s.Lozinka == playervm.Lozinka);
                    // System.Web.Security.FormsAuthentication.SetAuthCookie(playervm.KorisnickoIme, false);
                    Session["korisnickoIme"] = playervm.KorisnickoIme.ToString();
                    Session["lozinka"] = playervm.Lozinka.ToString();

                    MvcApplication.LoginCounter.ClearLockedLoginsAfterSuccessfull(playervm.KorisnickoIme);


                    return RedirectToAction("LoginSuccess");

                }
                else
                {
                    MvcApplication.LoginCounter.CheckLogin(playervm.KorisnickoIme);
                    if (MvcApplication.LoginCounter.ReturnIfLockedUsername(playervm.KorisnickoIme))
                    {
                        ViewBag.ErrorMessageLogin = MvcApplication.LoginCounter.LoginErrorMessage;
                        return View();
                    }

                }
                return View();


            }
            catch (Exception ex)
            {
                TempData["Message"] = "Login failed.Error - " + ex.Message;
            }
            return View();


        }






        public ActionResult LoginSuccess()
        {
            AccountViewModel playervm = new AccountViewModel();

            //if (Request.Cookies["cookie"] != null)
            //{
            //    Response.Cookies["cookie"].Expires = DateTime.Now.AddMinutes(1);
            //}


            HttpCookie hc = Request.Cookies["cookie"];
            if (hc != null)
            {
                playervm.KorisnickoIme = hc["cookie"];
                playervm.Lozinka = hc["cookie"];

                Response.Cookies["cookie"].Expires = DateTime.Now.AddMinutes(1);
                Response.Cookies.Add(hc);

                Response.Redirect("LoginSuccess");
            }
            if (Session["korisnickoIme"] != null)
            {
                TempData["korisnickoime"] = "Login Success";
                return View();
            }
            else
            {
                return RedirectToAction("Login");
            }
        }



        public ActionResult Logout()
        {
            if (Session != null)
            {
                Session.Remove("korisnickoIme");
                Session.Remove("lozinka");

            }

            return View();
        }


        public ActionResult Delete(int id)
        {
            db = new PlayerDb();
            if (db.DeletePlayer(id))
            {
                return RedirectToAction("GetPlayer");

            }


            return Content("Greška pri brisanju iz baze");
        }

        public JsonResult UsernameExists(string username)
        {
            PlayerDb db = new PlayerDb();


            if (ModelState.IsValid)
            {
                List<AccountViewModel> lista = new List<AccountViewModel>();

                lista = db.Validate();
                foreach (var item in lista)
                {
                    if (String.Equals(username, Convert.ToString(item.KorisnickoIme), StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(false);
                    }
                }
            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Verify(string id)
        {
            if (string.IsNullOrEmpty(id) || (!Regex.IsMatch(id, @"[0-9a-f]{8}\-
                                     ([0-9a-f]{4}\-){3}[0-9a-f]{12}")))
            {
                ViewBag.Msg = "Not Good!!!";
                return View();
            }

            else
            {
                var user = Membership.GetUser(new Guid(id));

                if (!user.IsApproved)
                {
                    user.IsApproved = true;
                    Membership.UpdateUser(user);
                    FormsAuthentication.SetAuthCookie(user.UserName, false);
                    return RedirectToAction("GetPlayers", "Player");
                }
                else
                {
                    FormsAuthentication.SignOut();
                    ViewBag.Msg = "Account Already Approved";
                    return RedirectToAction("Login");
                }
            }
        }


    }
}
