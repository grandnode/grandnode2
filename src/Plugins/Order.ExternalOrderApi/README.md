# External Order API for GrandNode

This plugin lets external systems send orders to your GrandNode shop through a simple API.

## What It Does
- Takes orders from external systems via API
- Creates guest customers automatically
- Manages shipping and billing addresses
- Checks if products exist before placing orders
- Gives clear error messages
- Logs everything for troubleshooting

## Setup
1. Put this plugin in your `src/Plugins` folder
2. Build your solution
3. Go to Admin > Plugins > Local plugins
4. Find "External Order API" and click "Install"

## API Usage

### Endpoint
```
POST /api/order
```

### Request Format
Send a JSON request like this:

```json
{
  "page": 1,
  "size": 1,
  "totalPages": 1,
  "totalElements": 1,
  "content": [
    {
      "orderNumber": "EXT-12345",
      "grossAmount": 199.99,
      "customerEmail": "customer@example.com",
      "customerFirstName": "John",
      "customerLastName": "Doe",
      "shipmentAddress": {
        "firstName": "John",
        "lastName": "Doe",
        "address1": "123 Main Street",
        "city": "New York",
        "postalCode": "10001",
        "countryCode": "US",
        "phone": "123-456-7890"
      },
      "lines": [
        {
          "sku": "PRODUCT-SKU-123",
          "quantity": 2,
          "price": 99.99
        }
      ]
    }
  ]
}
```

### Required Fields
- `orderNumber`: Your external order ID
- `grossAmount`: Total order amount
- `customerEmail`: Customer email address
- `shipmentAddress`: Shipping address with firstName, lastName, address1, city, and countryCode
- `lines`: Product items with sku, quantity, and price

### Responses

**Success (200 OK)**
```json
{
  "success": true,
  "orderId": "01234567-89ab-cdef-0123-456789abcdef",
  "orderNumber": "GN-12345"
}
```

**Error (400 Bad Request)**
```json
{
  "errors": {
    "Order": [
      "Product with SKU 'INVALID-SKU' was not found"
    ]
  }
}
```

## How It Works

1. We check if all products exist in your store
2. We create or find the customer by email
3. We set up the shipping and billing addresses
4. We create the shopping cart items
5. We place the order using Cash On Delivery payment method
6. We return the order ID and number

## Error Handling

The API uses standard HTTP status codes:
- `200`: Everything went well
- `400`: Something's wrong with your request
- `500`: Something went wrong on our side

All errors are logged in GrandNode's log system.

## Technical Notes

- The default payment method is Cash On Delivery
- You can extend the plugin to support other payment methods
- All operations are logged for troubleshooting
