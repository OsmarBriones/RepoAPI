using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoAPI.Utils
{
	public class EnumUtils
	{
		public static TEnum[] GetValues<TEnum>() where TEnum : Enum
		{
			return (TEnum[])Enum.GetValues(typeof(TEnum));
		}
}
}
