using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPTU_ELibrary.Domain.Interfaces
{
	public interface IDatabaseInitializer
	{
		Task InitializeAsync();
		Task SeedAsync();
		Task TrySeedAsync();
	}
}
