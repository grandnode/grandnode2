﻿using Grand.Domain.Customers;

namespace Grand.Business.Core.Utilities.Customers;

/// <summary>
///     Customer registration request
/// </summary>
public class RegistrationRequest
{
    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="email">Email</param>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="passwordFormat">Password format</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="isApproved">Is approved</param>
    public RegistrationRequest(Customer customer, string email, string username,
        string password,
        PasswordFormat passwordFormat,
        string storeId,
        bool isApproved = true)
    {
        Customer = customer;
        Email = email;
        Username = username;
        Password = password;
        PasswordFormat = passwordFormat;
        StoreId = storeId;
        IsApproved = isApproved;
    }

    /// <summary>
    ///     Customer
    /// </summary>
    public Customer Customer { get; set; }

    /// <summary>
    ///     Email
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    ///     Username
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    ///     Password
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    ///     Password format
    /// </summary>
    public PasswordFormat PasswordFormat { get; set; }

    /// <summary>
    ///     Store identifier
    /// </summary>
    public string StoreId { get; set; }

    /// <summary>
    ///     Is approved
    /// </summary>
    public bool IsApproved { get; set; }
}