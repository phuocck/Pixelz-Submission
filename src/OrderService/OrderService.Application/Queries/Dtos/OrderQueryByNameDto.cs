using OrderService.Application.Queries.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Queries.Dtos
{
    public class OrderQueryByNameDto: PagedQuery
    {
        public string Name { get; set; }
    }
}
