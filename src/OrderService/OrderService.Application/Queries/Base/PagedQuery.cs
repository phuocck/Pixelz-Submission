using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Queries.Base
{
    public abstract class PagedQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        protected PagedQuery() { }

        protected PagedQuery(int page, int pageSize)
        {
            Page = page < 1 ? 1 : page;
            PageSize = pageSize < 1 ? 10 : pageSize;
        }
    }
}
