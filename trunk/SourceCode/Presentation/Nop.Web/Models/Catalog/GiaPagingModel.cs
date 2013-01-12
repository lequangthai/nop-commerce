using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nop.Web.Models.Catalog
{
    public class GiaPagingModel
    {
        public int Count = 10;
        public GiaPagingModel() { }
        public GiaPagingModel(int totalPages, int currentPage)
        {
            TotalPages = totalPages;
            CurrentPage = currentPage;
            HasPaging = TotalPages > 1;
            if (!HasPaging)
            {
                var centerIndex = currentPage - Count / 2 - ((currentPage + Count / 2) > totalPages ? (currentPage + Count / 2 - totalPages) : 0);
                //var centerIndex = currentPage - pageCountShowing / 2 - ((currentPage + pageCountShowing / 2) > totalPage ? currentPage + (pageCountShowing / 2 - totalPage) : 0);
                PageStartingShow = centerIndex < 0 ? 0 : centerIndex;
                PageStartingShow++; // start with 1
            }
        }
        public bool HasPaging { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public string BaseUrl { get; set; }
        public int PageStartingShow { get; set; }

    }
}