using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pepperi.SDK.Model.Fixed;

namespace Pepperi.SDK.Model
{
	public class AccountInventory
	{
		 public Boolean? Hidden 	{get; set; }
		 public Int64? InStockQuantity 	{get; set; }
		 public DateTime? ModificationDateTime 	{get; set; }
		 public Reference<Account> Account { get; set; }
		 public Reference<Item> Item { get; set; }
	}
}
