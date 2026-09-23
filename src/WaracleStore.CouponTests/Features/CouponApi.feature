@API @AC2 @AC3 @AC4 @AC5 @AC6
Feature: Coupon pricing through the API
  The web app asks POST /api/cart/summary for every price it shows and POST /api/orders to
  place the order. Checking that contract directly separates "the pricing engine is wrong"
  from "the screen is wrong", and it runs without a browser.

  @Positive @KnownDefect
  Scenario Outline: AC-2 – The cart summary applies 25% for WARACLE25
    When I request a cart summary for "<Basket>" with coupon "WARACLE25"
    Then the response status is 200
    And the response reports the coupon as applied
    And the response is priced according to the release rules

    Examples:
      | Basket                               |
      | 1 x Waracle Cap                      |
      | 2 x Waracle Headset, 1 x Waracle Cap |

  @Positive
  Scenario: AC-3 / AC-4 – A basket without a coupon is priced as subtotal plus £5.00 shipping
    When I request a cart summary for "3 x Men's Logo T-Shirt"
    Then the response status is 200
    And the response reports the coupon as not applied
    And the response is priced according to the release rules

  @Edge
  Scenario: AC-3 – An empty basket has no shipping and no discount
    When I request a cart summary for an empty basket with coupon "WARACLE25"
    Then the response status is 200
    And the response is priced according to the release rules

  @Negative
  Scenario Outline: AC-5 – An invalid or empty code is reported as not applied with no discount
    When I request a cart summary for "1 x Waracle Headset" with coupon "<Code>"
    Then the response status is 200
    And the response reports the coupon as not applied
    And the response has no discount

    Examples:
      | Code      |
      | WARACLE50 |
      | SAVE25    |
      |           |

  @Edge
  Scenario Outline: AC-2 – The launch code is accepted regardless of letter case through the API
    When I request a cart summary for "1 x Waracle Cap" with coupon "<Code>"
    Then the response status is 200
    And the response reports the coupon as applied

    Examples:
      | Code      |
      | waracle25 |
      | Waracle25 |

  @Edge
  Scenario: AC-2 – Spaces around the launch code are ignored through the API
    When I request a cart summary for "1 x Waracle Cap" with coupon "WARACLE25" surrounded by spaces
    Then the response status is 200
    And the response reports the coupon as applied

  @Negative
  Scenario: A basket with an unknown product is rejected
    When I request a cart summary for an unknown product
    Then the response status is 400
    And the response message mentions "Unknown product"

  @Positive @KnownDefect
  Scenario: AC-6 – A placed order records the coupon and the discounted amounts
    When I place an order for "2 x Waracle Headset" with coupon "WARACLE25"
    Then the response status is 201
    And the order records the coupon "WARACLE25"
    And the response is priced according to the release rules

  @Positive
  Scenario: AC-6 – The stored order can be read back with the same coupon and amounts
    When I place an order for "1 x Waracle Cap" with coupon "WARACLE25"
    And I fetch the order I just placed
    Then the response status is 200
    And the fetched order matches the placed order

  @Positive
  Scenario: AC-6 – An order placed without a coupon records no coupon
    When I place an order for "1 x Waracle Cap" without a coupon
    Then the response status is 201
    And the order records no coupon
    And the response is priced according to the release rules

  @Edge
  Scenario: AC-6 – The order is charged exactly what the cart summary quoted
    When I place an order for "2 x Waracle Headset, 1 x Waracle Cap" with coupon "WARACLE25"
    Then the order amounts match the cart summary for the same basket and coupon

  @Negative
  Scenario: Placing an order requires a signed-in customer
    When I place an order for "1 x Waracle Cap" with coupon "WARACLE25" without signing in
    Then the response status is 401
    And the response message mentions "Authentication required"
