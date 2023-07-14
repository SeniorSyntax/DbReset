using System;
using DbAgnostic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace DbReset.Test;

public class ApplicableToSqlServerAttribute : Attribute, ITestAction
{
	public ActionTargets Targets { get; }

	public void BeforeTest(ITest test)
	{
		TestConnectionString.ConnectionString
			.PickAction(
				() => { },
				() => Assert.Ignore("Applicable to SqlServer"))
			;
	}

	public void AfterTest(ITest test)
	{
	}
}
