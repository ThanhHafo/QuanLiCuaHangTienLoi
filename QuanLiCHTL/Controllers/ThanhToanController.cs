using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLiCHTL.Models;
using System.Data.SqlClient;
using System.Text;
namespace QuanLiCHTL.Controllers
{
    public class ThanhToanController : Controller
    {
        dbQLCHTLDataContext data = new dbQLCHTLDataContext();
        // GET: ThanhToan
        public ActionResult Index()
        {
            var hoadon = data.chitiethoadons.Where(p => p.id_hd == GetMaxId("id", "hoadon"));
            if (hoadon != null)
            {
                ViewBag.kh = Session["tenkh"];
                ViewBag.diem = Session["diemkh"];
                ViewBag.soluong = hoadon.Sum(p => p.soluong);
                ViewBag.ttien = hoadon.Sum(p => p.gia * p.soluong);
                if(Session["giamgia"] != null)
                {
                    string giamgia = Session["giamgia"].ToString();
                    ViewBag.tcong = hoadon.Sum(p => p.thanhtien)- int.Parse(giamgia);
                    ViewBag.ck = giamgia;
                }
                else
                {
                    ViewBag.tcong = hoadon.Sum(p => p.thanhtien);
                }
            }

            var timhd = data.hoadons.ToList();
            if(timhd.Count()>1)timhd.RemoveAt(timhd.Count - 1);
            ViewBag.timhd = timhd;
            return View(hoadon);
        }

        public DateTime GetdateVN()
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vnNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, vnTimeZone);
            return vnNow;
        }
        public ActionResult DsHD(chitiethoadon hd,hoadon hdon, FormCollection f)
        {
            if (data.chitiethoadons.Any(p => p.barcode == int.Parse(f["hdbc"]) && p.id_hd == GetMaxId("id", "hoadon")))
            {
                var hoadon = data.chitiethoadons.SingleOrDefault(p => p.barcode == int.Parse(f["hdbc"]) && p.id_hd == GetMaxId("id", "hoadon"));
                hoadon.soluong += 1;
                hoadon.thanhtien = hoadon.gia * hoadon.soluong;
                data.SubmitChanges();
            }
            else
            {
                var products = data.sanphams.SingleOrDefault(p => p.barcode == int.Parse(f["hdbc"]));
                if (products != null)
                {
                    hd.id = GetMaxId("id", "chitiethoadon") + 1;
                    hd.barcode = products.barcode;
                    hd.gia = products.gia;
                    hd.soluong = 1;
                    hd.tensp = products.tensp;
                    hd.thanhtien = hd.gia * hd.soluong;
                    if(GetMaxId("id","hoadon")==0)
                    {
                        hdon.id = GetMaxId("id", "hoadon") + 1;
                        hd.id_hd = GetMaxId("id", "hoadon") + 1;
                        data.hoadons.InsertOnSubmit(hdon);
                    }
                    else
                    {
                        hd.id_hd = GetMaxId("id", "hoadon");
                    }
                    data.chitiethoadons.InsertOnSubmit(hd);
                    data.SubmitChanges();
                }
            }
            return RedirectToAction("Index");
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
        [HttpGet]
        public ActionResult HuyHD()
        {
            return View();
        }
        [HttpPost]
        public ActionResult HuyHD(huyhoadon hh,FormCollection f)
        {
            var idnv = Session["idnvien"].ToString();
            if (GetMaxId("id", "huyhoadon") == 0)
            {
                string connectionString = "Data Source=ThanhHao\\SQLEXPRESS;Initial Catalog=QLCHTL;Integrated Security=True";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sqlQuery = $"Insert Into huyhoadon(id) values({GetMaxId("id", "huyhoadon") + 1})";
                    SqlCommand command = new SqlCommand(sqlQuery, connection);
                    command.ExecuteNonQuery();
                }
            }
            var sp = data.chitiethoadons.Where(p => p.id_hd == GetMaxId("id", "hoadon")).ToList();
            if (sp != null)
            {
                foreach(var item in sp)
                {
                    item.id_huy = GetMaxId("id","huyhoadon");
                    item.id_hd = null;
                }
                var huy = data.huyhoadons.SingleOrDefault(p => p.id == GetMaxId("id", "huyhoadon"));
                huy.ngayhuy = GetdateVN();
                huy.tongtien = sp.Sum(p => p.gia*p.soluong);
                huy.lydo = f["lydo"];
                huy.idnv = int.Parse(idnv);
                data.SubmitChanges();

                string connectionString = "Data Source=ThanhHao\\SQLEXPRESS;Initial Catalog=QLCHTL;Integrated Security=True";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sqlQuery = $"Insert Into huyhoadon(id) values({GetMaxId("id", "huyhoadon") + 1})";
                    SqlCommand command = new SqlCommand(sqlQuery, connection);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult thanhtoanhd(sanpham sp, FormCollection f)
        {
            var idnv = Session["idnvien"].ToString();
            var ct = data.chitiethoadons.Where(p => p.id_hd == GetMaxId("id", "hoadon")).ToList();
            var barcode = ct.Select(p => p.barcode).ToList();
            var product = data.sanphams.Where(p => barcode.Contains(p.barcode)).ToList();
            if (int.Parse(f["tienkh"]) >= int.Parse(f["tientt"]))
            {
                string sdt = Session["sdtkh"].ToString();
                var kh = data.khachhangs.SingleOrDefault(p => p.sdt == int.Parse(sdt));
                var hd = data.hoadons.SingleOrDefault(p=>p.id== GetMaxId("id", "hoadon"));
                hd.tongtien = int.Parse(f["tientt"]);
                hd.soluong = ct.Sum(p => p.soluong);
                hd.ngaythanhtoan = GetdateVN();
                hd.idnv = int.Parse(idnv);
                kh.tichdiem += int.Parse(f["tientt"]) / 1000;
                if (Session["bcvoucher"] != null)
                {
                    string bcvoucher = Session["bcvoucher"].ToString();
                    var vch = data.vouchers.SingleOrDefault(p => p.barcode == int.Parse(bcvoucher));
                    vch.luotsudung -= 1;
                    hd.voucher = vch.barcode;
                }
                
                foreach (var sanpham in product)
                {
                    var ctItem = ct.SingleOrDefault(p => p.barcode == sanpham.barcode);
                    if (ctItem != null)
                    {
                        sanpham.soluong -= ctItem.soluong;
                    }
                }

                data.SubmitChanges();
                Session["giamgia"] = null;
                Session["bcvoucher"] = null;
                Session["tenkh"] = null;
                Session["diemkh"] = null;
                string connectionString = "Data Source=ThanhHao\\SQLEXPRESS;Initial Catalog=QLCHTL;Integrated Security=True";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sqlQuery = $"Insert Into hoadon(id) values({GetMaxId("id", "hoadon") + 1})";
                    SqlCommand command = new SqlCommand(sqlQuery, connection);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult voucher(FormCollection f)
        {
            DateTime ngay = GetdateVN().Date;
            var vch = data.vouchers.SingleOrDefault(p => p.barcode == int.Parse(f["vbc"])&&p.ngaysudung<=ngay &&p.hsd>ngay &&p.luotsudung>0);
            if (vch != null)
            {
                Session["giamgia"] = vch.giamgia;
                Session["bcvoucher"] = vch.barcode;
            }
            return RedirectToAction("Index");
        }

        public ActionResult capnhat(FormCollection f)
        {
            var cn = data.chitiethoadons.SingleOrDefault(p => p.barcode == int.Parse(f["bc"]) && p.id_hd == GetMaxId("id", "hoadon"));
            if (cn != null)
            {
                cn.soluong = int.Parse(f["sl"]);
                cn.thanhtien = cn.gia * int.Parse(f["sl"]);
                data.SubmitChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Xoasp(FormCollection f)
        {
            var productToRemove = data.chitiethoadons.SingleOrDefault(p => p.barcode == int.Parse(f["xoa"]) && p.id_hd==GetMaxId("id","hoadon"));
            if (productToRemove != null)
            {
                data.chitiethoadons.DeleteOnSubmit(productToRemove);
                data.SubmitChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult khachhang(FormCollection f)
        {
            var kh = data.khachhangs.SingleOrDefault(p => p.sdt == int.Parse(f["sdt"]));
            if (kh != null)
            {
                if (kh.tichdiem == null)
                {
                    kh.tichdiem = 0;
                    Session["diemkh"] = kh.tichdiem;
                    Session["sdtkh"] = kh.sdt;
                    Session["tenkh"] = kh.hoten;
                    data.SubmitChanges();
                }
                else
                {
                    Session["diemkh"] = kh.tichdiem;
                    Session["sdtkh"] = kh.sdt;
                    Session["tenkh"] = kh.hoten;
                }
            }
            
            return RedirectToAction("Index");
        }

        public ActionResult dangki(FormCollection f,khachhang kh)
        {
            if (ModelState.IsValid)
            {
                kh.id = GetMaxId("id", "khachhang") + 1;
                kh.hoten = f["ten"];
                kh.sdt = int.Parse(f["sdt"]);
                kh.tichdiem = 0;
                data.khachhangs.InsertOnSubmit(kh);
                data.SubmitChanges();
            }
            return RedirectToAction("Index");
        }

    }
  
}