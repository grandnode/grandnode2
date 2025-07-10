# External Order API for GrandNode

This plugin allows external systems to create orders in GrandNode via a simple REST API endpoint.

## API Documentation

### Endpoint

```
POST /api/order
```

### Request Format

The API accepts JSON data in two formats:

#### Format 1: Direct Order Object

```json
{
  "orderNumber": "EXT12345",
  "grossAmount": 51.98,
  "totalDiscount": 25.99,
  "totalTyDiscount": 0,
  "customerEmail": "customer@example.com",
  "customerFirstName": "John",
  "customerLastName": "Doe",
  "customerId": 12345,
  "taxNumber": "123456789",
  "tcIdentityNumber": "6789012345",
  "identityNumber": "6789012345",
  "shipmentAddress": {
    "id": 12345,
    "firstName": "John",
    "lastName": "Doe",
    "company": "Company Name",
    "address1": "123 Main St",
    "address2": "Apt 4B",
    "city": "New York",
    "district": "Manhattan",
    "districtId": 101,
    "postalCode": "10001",
    "countryCode": "US",
    "neighborhood": "Downtown",
    "neighborhoodId": 505,
    "phone": "555-123-4567",
    "fullName": "John Doe",
    "fullAddress": "123 Main St, Apt 4B, New York, NY 10001",
    "taxOffice": "Central Tax Office",
    "taxNumber": "123456789",
    "addressLines": {
      "addressLine1": "123 Main St",
      "addressLine2": "Apt 4B"
    },
    "stateName": "New York"
  },
  "invoiceAddress": {
    "id": 12346,
    "firstName": "John",
    "lastName": "Doe",
    "company": "Company Name",
    "address1": "123 Main St",
    "address2": "Apt 4B",
    "city": "New York",
    "district": "Manhattan",
    "districtId": 101,
    "postalCode": "10001",
    "countryCode": "US",
    "neighborhood": "Downtown",
    "neighborhoodId": 505,
    "phone": "555-123-4567",
    "fullName": "John Doe",
    "fullAddress": "123 Main St, Apt 4B, New York, NY 10001",
    "taxOffice": "Central Tax Office",
    "taxNumber": "123456789",
    "addressLines": {
      "addressLine1": "123 Main St",
      "addressLine2": "Apt 4B"
    },
    "stateName": "New York"
  },
  "lines": [
    {
      "sku": "PROD-001",
      "productName": "Product Name",
      "quantity": 2,
      "price": 25.99,
      "amount": 51.98,
      "discount": 0,
      "tyDiscount": 0,
      "currencyCode": "USD",
      "id": 7890,
      "barcode": "1234567890123",
      "productCode": 9876,
      "productSize": "M",
      "productColor": "Blue",
      "productOrigin": "USA",
      "merchantId": 456,
      "merchantSku": "MERCH-001",
      "salesCampaignId": 789,
      "orderLineItemStatusName": "Processing",
      "vatBaseAmount": 48.13,
      "productCategoryId": 15,
      "laborCost": 5.50,
      "discountDetails": [
        {
          "lineItemPrice": 25.99,
          "lineItemDiscount": 0,
          "lineItemTyDiscount": 0
        }
      ],
      "fastDeliveryOptions": [
        {
          "type": "Express"
        }
      ]
    }
  ],
  "orderDate": 1720003200000,
  "currencyCode": "USD",
  "status": "Pending",
  "shipmentPackageStatus": "Processing",
  "id": 8765,
  "cargoTrackingNumber": 1234567890,
  "paymentMethod": "Payments.PayInStore",
  "cargoTrackingLink": "https://tracking.example.com/1234567890",
  "cargoProviderName": "Express Shipping",
  "deliveryType": "Standard",
  "totalPrice": 51.98,
  "packageHistories": [
    {
      "createdDate": 1720003200000,
      "status": "Created"
    }
  ],
  "commercial": false,
  "giftBoxRequested": false
}
```

#### Format 2: Payload with Content Array

```json
{
  "page": 0,
  "size": 20,
  "totalPages": 1,
  "totalElements": 1,
  "content": [
    {
      "orderNumber": "EXT12345",
      "grossAmount": 51.98,
      "totalDiscount": 25.99,
      "totalTyDiscount": 0,
      "customerEmail": "customer@example.com",
      "customerFirstName": "John",
      "customerLastName": "Doe",
      "lines": [
        {
          "sku": "PROD-001",
          "quantity": 2,
          "price": 25.99
          // ... other line item fields
        }
      ],
      "shipmentAddress": {
        "firstName": "John",
        "lastName": "Doe",
        "address1": "123 Main St",
        "city": "New York",
        "postalCode": "10001",
        "countryCode": "US",
        "phone": "555-123-4567"
        // ... other address fields
      }
      // ... other order fields
    }
  ]
}
```

### Required Fields

- `orderNumber`: Unique identifier for the order
- `grossAmount`: Total order amount
- `customerEmail`: Customer's email address
- `shipmentAddress`: Complete shipping address information
  - Must include `firstName`, `lastName`, `address1`, `city`, `postalCode`, `countryCode`
- `lines`: Array of order items
  - Each item must include `sku`, `quantity`, and `price`

### Optional Fields

- `paymentMethod`: System name of the payment method to use for the order (e.g. "Payments.PayInStore", "Payments.CashOnDelivery")
  - If not provided, the system will default to "Payments.PayInStore"

### Response Format

#### Success Response

```json
{
  "success": true,
  "orderId": "12345-67890",
  "orderNumber": 1001
}
```

#### Error Response

```json
{
  "success": false,
  "errors": ["Error message 1", "Error message 2"]
}
```

or 

```json
{
  "success": false,
  "error": "General error message"
}
```

## Integration Flow

1. The external system sends order data to the API endpoint
2. The plugin validates the request data
3. The plugin checks if all product SKUs exist in GrandNode
4. The plugin creates or gets the customer by email
5. The plugin creates shopping cart items for each line item in the order
6. The plugin sets the specified payment method or uses the default "Payments.PayInStore"
7. The plugin places the order in GrandNode
8. The plugin returns success or failure response

## Error Handling

The API logs all errors to GrandNode's logging system. Common errors include:

- Invalid request data (missing required fields)
- Products not found (invalid SKUs)
- Order creation issues

## Security Considerations

This API should be secured using appropriate authentication mechanisms. Consider implementing:

- API key authentication
- OAuth 2.0
- IP address restrictions

## Example Usage

```csharp
// Example C# code to call the API
using System.Net.Http;
using System.Text;
using System.Text.Json;

var client = new HttpClient();
client.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_API_KEY");

var orderData = new
{
    orderNumber = "EXT12345",
    grossAmount = 51.98,
    customerEmail = "customer@example.com",
    // ... other order data
};

var content = new StringContent(
    JsonSerializer.Serialize(orderData),
    Encoding.UTF8,
    "application/json");

var response = await client.PostAsync("https://your-store.com/api/order", content);
var responseBody = await response.Content.ReadAsStringAsync();
```

## Troubleshooting

- Ensure all required fields are provided in the request
- Check that product SKUs exist in GrandNode and are published
- Verify the customer email is in a valid format
