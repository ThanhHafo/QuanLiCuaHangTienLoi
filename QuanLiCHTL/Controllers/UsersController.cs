using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLiCHTL.Models;
using System.Data.SqlClient;
namespace QuanLiCHTL.Controllers
{
    public class UsersController : Controller
    {
        dbQLCHTLDataContext data = new dbQLCHTLDataContext();
        // GET: Users
        [HttpGet]
        public ActionResult DangNhapUser()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DangNhapUser(FormCollection f, chamcong vaolam)
        {
            //Gán các giá trị người dùng nhập liệu cho các biến
            var sTenDN = f["UserName"];
            var sMatKhau = f["Password"];
            //Gán giá trị cho đối tượng được tạo mới (ad)
            nhanvien nv = data.nhanviens.SingleOrDefault(n => n.id == int.Parse(sTenDN) && n.matkhau == sMatKhau);
            if (nv != null)
            {
                Session["idnvien"] = nv.id;
                Session["nvien"] = nv.hoten;
                vaolam.id = GetMaxId("id","chamcong")+1;
                vaolam.idnv = nv.id;
                vaolam.giovaolam = GetdateVN();
                data.chamcongs.InsertOnSubmit(vaolam);
                data.SubmitChanges();
                return RedirectToAction("Index","ThanhToan");
            }
            else
            {
                ViewBag.ThongBao = "Tên đăng nhập hoặc mật khẩu không đúng";
            }
            return View();
        }

        public int GetMaxId(string tencot, string tenbang)
        {
            int maxId = 0;
            string connectionString = "Data Source=ThanhHao\\SQLEXPRESS;Initial Catalog=QLCHTL;Integrated Security=True";
            string columnName = tencot;
            string tableName = tenbang;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = $"SELECT MAX({columnName}) FROM {tableName}";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    object result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        maxId = Convert.ToInt32(result);
                    }
                }
            }
            return maxId;
        }

        public DateTime GetdateVN()
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vnNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, vnTimeZone);
            return vnNow;
        }
        [HttpGet]
        public ActionResult chamcong()
        {
            return View();
        }
        [HttpPost]
        public ActionResult chamcong(FormCollection f)
        {
            var sTenDN = f["nUserName"];
            var sMatKhau = f["nPassword"];
            DateTime hnay = GetdateVN().Date;
            //Gán giá trị cho đối tượng được tạo mới (ad)
            nhanvien nv = data.nhanviens.SingleOrDefault(n => n.id == int.Parse(sTenDN) && n.matkhau == sMatKhau);
            if (nv != null)
            {
                var giovaolamMin = data.chamcongs.Where(p => p.idnv == nv.id&&p.giorave==null&&p.giovaolam.Value.Date==hnay).OrderBy(p => p.giovaolam).Select(p => p.giovaolam).FirstOrDefault();
                var checkout = data.chamcongs.SingleOrDefault(p => p.idnv == nv.id && p.giovaolam.Value.Date == hnay && p.giovaolam == giovaolamMin);
                checkout.giorave = GetdateVN();
                data.SubmitChanges();
                return RedirectToAction("Index", "ThanhToan");
            }
            else
            {
                ViewBag.ThongBao = "Tên đăng nhập hoặc mật khẩu không đúng";
            }
            return View();
        }

        public ActionResult ketca()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ketca(FormCollection f)
        {
            var idnv = Session["idnvien"].ToString();
            var kc = data.chamcongs.SingleOrDefault(p => p.idnv == int.Parse(idnv)&&p.giorave!=null&&p.giovaolam.Value.Day==GetdateVN().Day
                                                    && p.giovaolam.Value.Month==GetdateVN().Month && p.giovaolam.Value.Year==GetdateVN().Year&&p.ketca==null);
            kc.ketca = int.Parse(f["tongkc"]);
            var giovaolam = kc.giovaolam;
            var giorave = kc.giorave;
            var dt = data.hoadons.Where(p => p.ngaythanhtoan > giovaolam && p.ngaythanhtoan < giorave);
            kc.doanhthu = dt.Sum(p => p.tongtien);

            data.SubmitChanges();
            return RedirectToAction("DangNhapUser");
        }
    }
}