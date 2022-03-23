using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ECommerceLiteBLL.Repository;
using ECommerceLiteEntity.ViewModels;

namespace ECommerceLiteUI.Controllers
{
    public class ChartController : Controller
    {
        //Global alan
        CategoryRepo myCategoryRepo = new CategoryRepo();
        public ActionResult VisualizePieChartResult()
        {
            //Piechartta göstermek istediğimiz data'yı alacağız
            //Bu datayı Dashboad'daki ajax işlemine gönderebilmek için
            // Return Json ile işlem yapacağız.
            var data = myCategoryRepo.GetBaseCategoriesProductCount();
            return Json(data,JsonRequestBehavior.AllowGet);
        }

    }
}