using System;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using PaymentManager.Application.Common;

namespace PaymentManager.WebApi.Tests.Integration;

public static class TestLifecycle
{
    public static async Task TearDown(IPaymentManagerContext context)
    {
        await context.Database.EnsureDeletedAsync();
    }
}
