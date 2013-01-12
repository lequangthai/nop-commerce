using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nop.Web.Models.Catalog
{
    public class CatalogIndexModel
    { 
        public string catName { get; set; }
        public string catType { get; set; }
        public int pageIndex { get; set; }
        public bool isHomePage { get; set; }
        public int pageItems { get; set; }
    }
}