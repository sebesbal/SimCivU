using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimcivEngine
{
    // Classes for trade. They work on one single product type.
    namespace Economy
    {
        class NodeTradeData
        { }

        class EdgeTradeData
        { }

        class Trader
        {
            public Product product;
        }

        class Seller : Trader
        {
            public Seller() { }
        }

        class Buyer : Trader
        {
        }

    }
}
