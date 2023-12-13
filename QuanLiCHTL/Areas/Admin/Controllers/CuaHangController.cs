using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLiCHTL.Models;
using System.Data.SqlClient;
using System.Text;
using OfficeOpenXml;
using System.IO;
using ZXing;
using System.Drawing;
namespace QuanLiCHTL.Areas.Admin.Controllers
{
    public class CuaHangController : Controller
    {
        dbQLCHTLDataContext data = new dbQLCHTLDataContext();
        // GET: CuaHang
        public ActionResult Index()
        {
            ViewBag.dthu = 0;
            ViewBag.sobill = 0;
            string connectionString = "Data Source=ThanhHao\\SQLEXPRESS;Initial Catalog=QLCHTL;Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = $"Select  SUM(tongtien) from hoadon where CONVERT(date,ngaythanhtoan) = CONVERT(date,GETDATE())";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    object result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        ViewBag.dthu = Convert.ToInt32(result);
                    }
                }
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = $"Select  COUNT(*) from hoadon where CONVERT(date,ngaythanhtoan) = CONVERT(date,GETDATE())";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    object result2 = command.ExecuteScalar();
                    if (result2 != DBNull.Value)
                    {
                        ViewBag.sobill = Convert.ToInt32(result2);
                    }
                }
            }

            var nvien = data.nhanviens.Count();
            ViewBag.nv = nvien;

            return View();
        }

        private List<sanpham> DsSanpham()
        {
            return data.sanphams.ToList();
        }
        [HttpGet]
        public ActionResult Masanpham()
        {
            var products = from p in data.inbarcodes select p;
            return View("Masanpham", products.ToList());
        }

        [HttpPost]
        public ActionResult Masanpham(inbarcode inbc, FormCollection f)
        {
            if (ModelState.IsValid)
            {
                inbc.id = GetMaxId("id", "inbarcode") + 1;
                inbc.barcode = int.Parse(f["inbarcode"]);
                inbc.tensp = GetTensp(int.Parse(f["inbarcode"]));
                data.inbarcodes.InsertOnSubmit(inbc);
                data.SubmitChanges();
            }
            return RedirectToAction("Masanpham");
        }

        public ActionResult Quanlisanpham(string tim)
        {
            var products = from p in data.sanphams select p;

            if (!string.IsNullOrEmpty(tim))
            {
                products = products.Where(p => p.tensp.Contains(tim) || p.mota.Contains(tim));
            }

            return View(products.ToList());
        }

        public ActionResult Themsanpham(string tim)
        {
            var products = from p in data.sanpham_nccs select p;
            var getdate = GetdateVN();
            var ngay7 = GetdateVN().AddDays(-6);
            var ds = data.lichsus.Where(p => p.loaicapnhat == "them" && p.ngaycapnhat >= ngay7 && p.ngaycapnhat <= getdate);
            var ds_ncc = data.sanpham_nccs.ToList();
            ViewBag.ncc = ds_ncc;
            ViewBag.ls = ds;
            if (!string.IsNullOrEmpty(tim))
            {
                products = products.Where(p => p.barcode == int.Parse(tim));
                Session["bct"] = tim;
                return View(products.ToList());
            }
            return View(products.Take(0));
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Them(sanpham sp, lichsu ls, FormCollection f)
        {
            var barcode = Session["bct"] as string;
            if (ModelState.IsValid)
            {
                sp.barcode = int.Parse(barcode);
                sp.tensp = f["sTenSp"];
                sp.mota = f["sMoTa"];
                sp.gia = int.Parse(f["sGia"]);
                sp.soluong = 0;

                ls.barcode = sp.barcode;
                ls.tensp = sp.tensp;
                ls.ngaycapnhat = GetdateVN();
                ls.loaicapnhat = "them";
                ls.id = GetMaxId("id", "lichsu") + 1;

                if (data.sanpham_nccs.Any(s => s.barcode == sp.barcode))
                {
                    if (data.sanphams.Any(p => p.barcode == sp.barcode))
                    {
                        ModelState.AddModelError(string.Empty, "Có lỗi trong quá trình nhập liệu.");
                        return View("Quanlisanpham", sp);
                    }
                    else
                    {
                        data.sanphams.InsertOnSubmit(sp);
                        data.lichsus.InsertOnSubmit(ls);
                        data.SubmitChanges();
                    }
                }
            }
            return RedirectToAction("Quanlisanpham");
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

        public int GetCount(string tencot, string tenbang, int giatri)
        {
            int maxId = 0;
            string connectionString = "Data Source=ThanhHao\\SQLEXPRESS;Initial Catalog=QLCHTL;Integrated Security=True";
            string columnName = tencot;
            string tableName = tenbang;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = $"SELECT COUNT(*) FROM {tableName} Where {tencot} = {giatri}";
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

        public ActionResult Suasanpham(string sua)
        {
            var products = from p in data.sanphams select p;
            var getdate = GetdateVN();
            var ngay7 = GetdateVN().AddDays(-6);
            var ds = data.lichsus.Where(p => p.loaicapnhat == "sua" && p.ngaycapnhat >= ngay7 && p.ngaycapnhat <= getdate);
            ViewBag.dssua = ds;
            if (!string.IsNullOrEmpty(sua))
            {
                products = products.Where(p => p.barcode == int.Parse(sua));
                Session["key"] = sua;
                return View(products.ToList());
            }
            return View(products.Take(0));
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Sua(lichsu ls, FormCollection f)
        {
            var datasua = Session["key"] as string;
            var sanpham = data.sanphams.SingleOrDefault(p => p.barcode == int.Parse(datasua));
            if (ModelState.IsValid)
            {
                sanpham.tensp = f["cstensp"];
                sanpham.mota = f["csmota"];
                sanpham.gia = double.Parse(f["csgia"]);
                sanpham.soluong = int.Parse(f["cstonkho"]);
                ls.barcode = int.Parse(datasua);
                ls.tensp = f["cstensp"];
                ls.ngaycapnhat = GetdateVN();
                ls.loaicapnhat = "sua";
                ls.id = GetMaxId("id", "lichsu") + 1;
                data.lichsus.InsertOnSubmit(ls);
                data.SubmitChanges();
                return RedirectToAction("Quanlisanpham");
            }
            return View();
        }


        public ActionResult dathang()
        {
            var duoi5 = data.sanphams.Where(p => p.soluong < 5).ToList();
            ViewBag.duoi5 = duoi5;
            int idpn = GetMaxId("id", "phieunhap");
            var products = from p in data.dathangs where (p.idphieunhap == idpn) select p;
            ViewBag.tong = data.dathangs.Where(s => s.idphieunhap == idpn).Sum(p => p.gia * p.soluong);
            return View("dathang", products.ToList());
        }

        [HttpPost]
        public ActionResult dathang(dathang dh, phieunhap pn, FormCollection f)
        {
            if (ModelState.IsValid)
            {
                if (data.dathangs.Any(code => code.barcode == int.Parse(f["dhbarcode"]) && code.idphieunhap == GetMaxId("id", "phieunhap") + 1))
                {
                    var them = data.dathangs.SingleOrDefault(p => p.barcode == int.Parse(f["dhbarcode"]));
                    them.soluong += int.Parse(f["slsp"]);
                    data.SubmitChanges();
                }
                else
                {
                    var sp = data.sanpham_nccs.SingleOrDefault(p => p.barcode == int.Parse(f["dhbarcode"]));
                    dh.gia = sp.dongia;
                    dh.id = GetMaxId("id", "dathang") + 1;
                    dh.barcode = int.Parse(f["dhbarcode"]);
                    dh.soluong = int.Parse(f["slsp"]);
                    dh.tensp = GetTensp(int.Parse(f["dhbarcode"]));
                    if (GetMaxId("id", "phieunhap") + 1 == 1)
                    {
                        pn.id = GetMaxId("id", "phieunhap") + 1;
                        dh.idphieunhap = pn.id;
                        data.phieunhaps.InsertOnSubmit(pn);
                    }
                    else
                    {
                        dh.idphieunhap = GetMaxId("id", "phieunhap");
                    }
                    data.dathangs.InsertOnSubmit(dh);
                    data.SubmitChanges();
                }
            }
            return RedirectToAction("dathang");
        }

        public ActionResult xacnhandh(phieunhap ph)
        {
            int idpn = GetMaxId("id", "phieunhap");
            int idpndh = GetMaxId("idphieunhap", "dathang");
            var dh = data.dathangs.Where(p => p.idphieunhap == idpndh).ToList();
            if (idpn == idpndh)
            {
                var pn = data.phieunhaps.SingleOrDefault(p => p.id == idpndh);
                pn.soluong = dh.Sum(p => p.soluong);
                pn.ngaydat = GetdateVN();
                pn.barcode = int.Parse(TaoMaNgauNhien(9));
                pn.tongtien = dh.Sum(p => p.gia * p.soluong);
                data.SubmitChanges();

                string connectionString = "Data Source=ThanhHao\\SQLEXPRESS;Initial Catalog=QLCHTL;Integrated Security=True";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sqlQuery = $"Insert Into phieunhap(id) values({GetMaxId("id", "phieunhap") + 1})";
                    SqlCommand command = new SqlCommand(sqlQuery, connection);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("dathang");
        }

        public string GetTensp(int barcode)
        {
            string ten = "";
            string connectionString = "Data Source=ThanhHao\\SQLEXPRESS;Initial Catalog=QLCHTL;Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sqlQuery = $"SELECT tensp FROM sanpham WHERE barcode = {barcode}";
                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    object result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        ten = result.ToString();
                    }
                }
            }
            return ten;
        }


        public ActionResult Xoadh()
        {
            string connectionString = "Data Source=ThanhHao\\SQLEXPRESS;Initial Catalog=QLCHTL;Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sqlQuery = $"Delete From dathang where idphieunhap = {GetMaxId("idphieunhap", "dathang")}";
                SqlCommand command = new SqlCommand(sqlQuery, connection);
                command.ExecuteNonQuery();
            }
            return RedirectToAction("dathang");
        }


        public ActionResult phieunhap()
        {
            var ds = data.phieunhaps.Where(p => p.ngaynhap == null && p.barcode != null);
            return View("phieunhap", ds.ToList());
        }
        [HttpPost]
        public ActionResult phieunhap(phieunhap ph, FormCollection f)
        {
            var pn = data.phieunhaps.SingleOrDefault(p => p.barcode == int.Parse(f["nbarcode"]));
            if (pn != null)
            {
                List<dathang> dh = data.dathangs.Where(p => p.idphieunhap == pn.id).ToList();
                for (int i = 0; i < dh.Count(); i++)
                {
                    var nh = data.sanphams.SingleOrDefault(s => s.barcode == dh[i].barcode);
                    nh.soluong += dh[i].soluong;
                    data.SubmitChanges();
                }
                pn.ngaynhap = GetdateVN();
                data.SubmitChanges();
            }
            return RedirectToAction("phieunhap");
        }

        private string TaoMaNgauNhien(int doDai)
        {
            const string kyTu = "1234567890";
            StringBuilder sb = new StringBuilder();

            Random random = new Random();

            for (int i = 0; i < doDai; i++)
            {
                int index = random.Next(kyTu.Length);
                sb.Append(kyTu[index]);
            }

            return sb.ToString();
        }

        public ActionResult lichsu(FormCollection f)
        {
            var ds = data.phieunhaps.Where(p => p.barcode != null);
            if (f["iddh"] != null)
            {
                var pn = data.phieunhaps.SingleOrDefault(p => p.barcode == int.Parse(f["iddh"]));
                var dh = data.dathangs.Where(p => p.idphieunhap == pn.id).ToList();
                ViewBag.xdh = dh;
            }
            return View(ds.ToList());
        }

        public DateTime GetdateVN()
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vnNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, vnTimeZone);
            return vnNow;
        }
        [HttpGet]
        public ActionResult doanhthu()
        {
            var dt = data.hoadons.Where(p => p.ngaythanhtoan.Value.Day == GetdateVN().Day && p.ngaythanhtoan.Value.Month == GetdateVN().Month &&
                                        p.ngaythanhtoan.Value.Year == GetdateVN().Year).ToList();
            ViewBag.dt = dt.Sum(p => p.tongtien);


            var ketca = data.chamcongs.Where(p => p.giovaolam.Value.Day == GetdateVN().Day && p.giovaolam.Value.Month == GetdateVN().Month
                                        && p.giovaolam.Value.Year == GetdateVN().Year && p.ketca != null).ToList();

            

            ViewBag.kc = ketca;
            ViewBag.tkc = ketca.Sum(p => p.ketca);
            ViewBag.ngay = "hôm nay";
            return View();
        }
        [HttpPost]
        public ActionResult doanhthu(FormCollection f)
        {
            
            DateTime tg = DateTime.Parse(f["ngay"]);

            var dt = data.hoadons.Where(p => p.ngaythanhtoan.Value.Day == tg.Day && p.ngaythanhtoan.Value.Month == tg.Month &&
                                        p.ngaythanhtoan.Value.Year == tg.Year).ToList();
            ViewBag.dt = dt.Sum(p => p.tongtien);


            var ketca = data.chamcongs.Where(p => p.giovaolam.Value.Day == tg.Day && p.giovaolam.Value.Month == tg.Month
                                        && p.giovaolam.Value.Year == tg.Year && p.ketca != null).ToList();
            ViewBag.kc = ketca;
            ViewBag.tkc = ketca.Sum(p => p.ketca);
            ViewBag.ngay = tg.Day+"/"+tg.Month+"/"+tg.Year;
            return View();
        }
        [HttpGet]
        public ActionResult doanhthuthang()
        {
            ViewBag.tongt = 0;
            ViewBag.tongsl = 0;
            return View();
        }
        [HttpPost]
        public ActionResult doanhthuthang(FormCollection f)
        {
            DateTime tg = DateTime.Parse(f["ngay"]);
            var dh = data.phieunhaps.Where(p => p.ngaydat.Value.Month == tg.Month && p.ngaydat.Value.Year == tg.Year);
            if (dh != null)
            {
                ViewBag.tongt = dh.Sum(p => p.tongtien);
                ViewBag.tongsl = dh.Sum(p => p.soluong);
                ViewBag.thang = tg.Month;
            }

            var dt = data.hoadons.Where(p => p.ngaythanhtoan.Value.Month == tg.Month && p.ngaythanhtoan.Value.Year == tg.Year);
            if (dt != null)
            {
                ViewBag.tongtt = dt.Sum(p => p.tongtien);
                ViewBag.tongsltt = dt.Sum(p => p.soluong);
            }

            var hh = data.huyhangs.Where(p => p.hsd.Value.Month == tg.Month && p.hsd.Value.Year == tg.Year && p.tinhtrang != null);
            if (hh != null)
            {
                ViewBag.tonghh = hh.Sum(p=>p.dongia*p.soluongdahuy);
            }
            return View();
        }
        [HttpGet]
        public ActionResult dshuyhang()
        {
            var hh = data.huyhangs.Where(p=>p.hsd.Value.Month==GetdateVN().Month && p.soluong!=0).ToList();
            return View(hh);
        }
        [HttpPost]
        public ActionResult dshuyhang(FormCollection f)
        {
            DateTime tg = DateTime.Parse(f["ngay"]);
            var hh = data.huyhangs.Where(p => p.hsd.Value.Month == tg.Month && p.soluong != 0).ToList();
            return View(hh);
        }
        [HttpPost]
        public ActionResult ImportExcel(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                if (data.huyhangs.Any())
                {
                    var newhh = data.huyhangs.Where(p => p.soluongdahuy == null).ToList();
                    foreach (var item in newhh)
                    {
                        item.soluongdahuy = 0;
                    }
                }
                try
                {
                    using (var package = new ExcelPackage(file.InputStream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;

                        for (int dong = 2; dong <= rowCount; dong++)
                        {
                            string barcode = worksheet.Cells[dong, 1].Value.ToString();
                            string tensp = worksheet.Cells[dong, 2].Value.ToString();
                            string hsd = worksheet.Cells[dong, 3].Value.ToString();
                            string soluong  = worksheet.Cells[dong, 4].Value.ToString();

                            huyhang hh = new huyhang();
                            hh.id = GetMaxId("id", "huyhang") + 1;
                            hh.barcode = int.Parse(barcode);
                            hh.tensp = tensp;
                            hh.hsd = DateTime.Parse(hsd);
                            hh.soluong = int.Parse(soluong);
                            data.huyhangs.InsertOnSubmit(hh);
                            data.SubmitChanges();
                        }
                    }

                    ViewBag.Message = "Nhập thành công!";
                }
                catch (Exception ex)
                {
                    ViewBag.Error = $"Lỗi: {ex.Message}";
                }
            }
            else
            {
                ViewBag.Error = "File không hợp lệ!";
            }

            return RedirectToAction("dshuyhang");
        }

        public ActionResult huyhang()
        {
            var hh = data.huyhangs.Where(p =>p.hsd.Value.Day-GetdateVN().Day==1 &&p.hsd.Value.Month==GetdateVN().Month
                                           && p.hsd.Value.Year==GetdateVN().Year).ToList();
            return View(hh);
        }

        public ActionResult hangdahuy()
        {
            var hdh = data.huyhangs.Where(p=>p.hsd.Value.Month==GetdateVN().Month && p.hsd.Value.Year==GetdateVN().Year && p.tinhtrang != null).ToList();
            ViewBag.chiphi = hdh.Sum(p => p.dongia*p.soluongdahuy);
            ViewBag.slhh = hdh.Sum(p => p.soluongdahuy);
            return View(hdh);
        }

        public ActionResult xacnhanhh(FormCollection f)
        {
            var hh = data.huyhangs.SingleOrDefault(p => p.barcode == int.Parse(f["bch"]) && p.hsd.Value.Day - GetdateVN().Day == 1 );
            var sp = data.sanpham_nccs.SingleOrDefault(p => p.barcode == int.Parse(f["bch"]));
            var spch = data.sanphams.SingleOrDefault(p => p.barcode == int.Parse(f["bch"]));
            hh.soluongdahuy = int.Parse(f["slh"]);
            hh.tinhtrang = "Đã hủy";
            hh.dongia = sp.dongia;
            spch.soluong -= hh.soluongdahuy;
            data.SubmitChanges();
            return RedirectToAction("huyhang");
        }


        public ActionResult ql_hoadon(FormCollection f)
        {
            if (f["ngay"] != null)
            { 
                Session["ngayhd"] = f["ngay"];
            }
            if(Session["ngayhd"]==null)
            {
                Session["ngayhd"] = GetdateVN();
            }
            var ngay = Session["ngayhd"].ToString();

            DateTime hnay = DateTime.Parse(ngay).Date;
            ViewBag.hd = data.hoadons.Where(p => p.ngaythanhtoan.Value.Date == hnay).ToList();
            ViewBag.ngay = hnay.Day + "/" + hnay.Month + "/" + hnay.Year;
            if (f["idhd"] != null)
            {
                var cthd = data.chitiethoadons.Where(p => p.id_hd == int.Parse(f["idhd"]));
                return View(cthd);
            }
            return View();
        }
        [HttpGet]
        public ActionResult hoadonhuy()
        {
            var huy = data.huyhoadons.Where(p=>p.ngayhuy.Value.Date==GetdateVN().Date);
            ViewBag.huy = huy;
            ViewBag.ngay = "hôm nay";
            return View();
        }
        [HttpPost]
        public ActionResult hoadonhuy(FormCollection f)
        {
            DateTime ngay = DateTime.Parse(f["ngay"]);
            ViewBag.ngay = ngay.Day + "/" + ngay.Month + "/" + ngay.Year;
            var huy = data.huyhoadons.Where(p => p.ngayhuy.Value.Date == ngay);
            ViewBag.huy = huy;
            
            return View();
        }

        public ActionResult DsNv()
        {
            var ds = data.nhanviens.ToList();
            ViewBag.ds = ds;
            return View();
        }
        [HttpGet]
        public ActionResult themnv()
        {

            return View();
        }
        [HttpPost]
        public ActionResult themnv(FormCollection f,nhanvien nv)
        {
            if (ModelState.IsValid)
            {
                nv.id = int.Parse(f["id"]);
                nv.hoten = f["hoten"];
                nv.sdt = int.Parse(f["sdt"]);
                nv.matkhau = f["mk"];
                data.nhanviens.InsertOnSubmit(nv);
                data.SubmitChanges();
            }
            return View();
        }

        public ActionResult suanv(string id)
        {
            var nv = from p in data.nhanviens select p;
            if (!string.IsNullOrEmpty(id))
            {
                nv = nv.Where(p => p.id == int.Parse(id));
                return View(nv.ToList());
            }
            return View(nv.Take(0));
        }

        public ActionResult xacnhansua(FormCollection f)
        {
            var nv = data.nhanviens.SingleOrDefault(p => p.id == int.Parse(f["id"]));
            nv.hoten = f["hoten"];
            nv.sdt = int.Parse(f["sdt"]);
            data.SubmitChanges();
            return RedirectToAction("DsNv");
        }

        public ActionResult ds_voucher()
        {
            var vch = data.vouchers.ToList();
            ViewBag.vch = vch;
            return View();
        }
        [HttpGet]
        public ActionResult them_voucher()
        {
            return View();
        }
        [HttpPost]
        public ActionResult them_voucher(voucher vch,FormCollection f)
        {
            vch.id = GetMaxId("id","voucher") + 1;
            vch.barcode = int.Parse(f["vbc"]);
            vch.mota = f["vmt"];
            vch.giamgia = int.Parse(f["vgg"]);
            vch.ngaysudung = DateTime.Parse(f["vnsd"]).Date;
            vch.hsd = DateTime.Parse(f["vhsd"]).Date;
            vch.luotsudung = int.Parse(f["vlsd"]);
            data.vouchers.InsertOnSubmit(vch);
            data.SubmitChanges();
            return View();
        }

        [HttpGet]
        public ActionResult sua_voucher(string barcode)
        {
            var vch = from p in data.vouchers select p;
            if (!string.IsNullOrEmpty(barcode))
            {
                vch = vch.Where(p => p.barcode == int.Parse(barcode));
                return View(vch.ToList());
            }
            return View(vch.Take(0));
        }
        [HttpPost]
        public ActionResult sua_voucher(voucher vch, FormCollection f)
        {
            var sua = data.vouchers.SingleOrDefault(p => p.barcode == int.Parse(f["vbc"]));
            sua.mota = f["vmt"];
            sua.giamgia = int.Parse(f["vgg"]);
            sua.ngaysudung = DateTime.Parse(f["vnsd"]).Date;
            sua.hsd = DateTime.Parse(f["vhsd"]).Date;
            sua.luotsudung = int.Parse(f["vlsd"]);
            data.SubmitChanges();

            return RedirectToAction("ds_voucher");
        }
        [HttpGet]
        public ActionResult xoa_voucher()
        {
            return View();
        }

        [HttpPost]
        public ActionResult xoa_voucher(FormCollection f)
        {
            var xoa = data.vouchers.SingleOrDefault(p => p.barcode == int.Parse(f["vx"]));
            data.vouchers.DeleteOnSubmit(xoa);
            data.SubmitChanges();
            return RedirectToAction("ds_voucher");
        }

        public ActionResult Mavach()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GenerateBarcode(string barcodeData)
        {
            // Tạo đối tượng BarcodeWriter
            BarcodeWriter barcodeWriter = new BarcodeWriter();
            barcodeWriter.Format = BarcodeFormat.CODE_128; // Chọn định dạng mã vạch

            // Tạo hình ảnh mã vạch từ dữ liệu đầu vào
            Bitmap barcodeBitmap = barcodeWriter.Write(barcodeData);

            // Lưu hình ảnh vào MemoryStream
            MemoryStream ms = new MemoryStream();
            barcodeBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            // Chuyển đổi MemoryStream thành mảng byte
            byte[] byteImage = ms.ToArray();

            // Lưu mảng byte hình ảnh mã vạch vào TempData để truyền qua view kết quả
            TempData["BarcodeImage"] = byteImage;

            // Chuyển hướng đến view kết quả
            return RedirectToAction("Result");
        }

        public ActionResult Result()
        {
            // Lấy mảng byte hình ảnh mã vạch từ TempData
            byte[] byteImage = TempData["BarcodeImage"] as byte[];

            // Truyền mảng byte hình ảnh mã vạch về view
            return File(byteImage, "image/png");
        }

        public ActionResult ds_khachhang()
        {
            var kh = data.khachhangs.ToList();
            ViewBag.kh = kh;
            return View();
        }
        [HttpGet]
        public ActionResult sua_khachhang(string sdt)
        {
            var kh = from p in data.khachhangs select p;
            if (!string.IsNullOrEmpty(sdt))
            {
                kh = kh.Where(p => p.sdt == int.Parse(sdt));
                return View(kh.ToList());
            }
            return View(kh.Take(0));
        }
        [HttpPost]
        public ActionResult sua_khachhang(FormCollection f)
        {
            var kh = data.khachhangs.SingleOrDefault(p => p.sdt == int.Parse(f["sdt"]));
            int id = kh.id;
            var sua = data.khachhangs.SingleOrDefault(p => p.id == id);
            sua.sdt = int.Parse(f["sdt"]);
            sua.hoten = f["hoten"];
            sua.tichdiem = int.Parse(f["diem"]);
            data.SubmitChanges();
            return RedirectToAction("ds_khachhang");
        }
    }
}