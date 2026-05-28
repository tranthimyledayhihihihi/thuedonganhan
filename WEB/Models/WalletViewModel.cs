using System.Collections.Generic;

namespace WEB.Models
{
    public class WalletViewModel
    {
        public decimal Balance { get; set; }
        public List<TransactionViewModel> Transactions { get; set; } = new List<TransactionViewModel>();
    }
}
