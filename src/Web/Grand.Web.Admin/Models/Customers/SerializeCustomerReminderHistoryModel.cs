﻿using System;

namespace Grand.Web.Admin.Models.Customers
{
    public class SerializeCustomerReminderHistoryModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public DateTime SendDate { get; set; }
        public int Level { get; set; }
        public bool OrderId { get; set; }
    }
}
