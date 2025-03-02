﻿using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class PrepareOrderCodeCommandHandler : IRequestHandler<PrepareOrderCodeCommand, string>
{
    private static readonly Random random = new();
    private readonly OrderSettings _orderSettings;

    public PrepareOrderCodeCommandHandler(OrderSettings orderSettings)
    {
        _orderSettings = orderSettings;
    }

    public async Task<string> Handle(PrepareOrderCodeCommand request, CancellationToken cancellationToken)
    {
        var length = _orderSettings.LengthCode;
        if (length == 0)
            length = 8;

        var code = RandomString(length);
        return await Task.FromResult(code);
    }

    private static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}