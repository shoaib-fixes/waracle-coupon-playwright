@AC3 @AC4
Feature: Order summary totals
  When a coupon is applied the order summary updates to show the discount, shipping and
  revised total.

  AC-3  Standard shipping of £5.00 applies to any non-empty basket.
  AC-4  Order total = subtotal − discount + shipping.

  Background:
    Given I am signed in as the demo customer

  @Positive
  Scenario Outline: AC-3 – Standard shipping of £5.00 applies to any non-empty basket
    Given my basket contains "<Basket>"
    When I open the cart
    Then the shipping charge shown is £5.00

    Examples:
      | Basket                                              |
      | 1 x Waracle Cap                                     |
      | 5 x Waracle Cap                                     |
      | 1 x Waracle Premium Headset, 2 x Women's Logo Shorts |

  @Positive
  Scenario: AC-3 – Shipping is not affected by the coupon
    Given my basket contains "1 x Waracle Headset"
    When I open the cart
    And I apply the coupon "WARACLE25"
    And the order summary shows a coupon line for "WARACLE25"
    Then the shipping charge shown is £5.00

  @Edge
  Scenario: AC-3 – An empty basket has nothing to ship and no order summary
    Given my basket is empty
    When I open the cart
    Then the cart shows the empty basket message
    And no order summary is shown

  @Positive
  Scenario Outline: AC-4 – Order total = subtotal + shipping when no coupon is applied
    Given my basket contains "<Basket>"
    When I open the cart
    Then the order summary shows:
      | Subtotal   | Discount | Shipping | Total   |
      | <Subtotal> | none     | £5.00    | <Total> |

    Examples:
      | Basket                                        | Subtotal | Total   |
      | 1 x Waracle Cap                               | £19.99   | £24.99  |
      | 2 x Waracle Headset, 1 x Waracle Cap          | £139.97  | £144.97 |
      | 1 x Men's Logo T-Shirt, 1 x Men's Logo Shorts | £54.98   | £59.98  |

  @Positive @KnownDefect
  Scenario Outline: AC-4 – Order total = subtotal − discount + shipping with WARACLE25 applied
    Given my basket contains "<Basket>"
    When I open the cart
    And I apply the coupon "WARACLE25"
    Then the order summary shows:
      | Subtotal   | Discount   | Shipping | Total   |
      | <Subtotal> | <Discount> | £5.00    | <Total> |

    Examples:
      | Basket                      | Subtotal | Discount | Total  |
      | 1 x Waracle Premium Headset | £79.99   | £20.00   | £64.99 |
      | 4 x Waracle Cap             | £79.96   | £19.99   | £64.97 |

  @Edge
  Scenario: AC-4 – Clearing the coupon restores the undiscounted total
    Given my basket contains "1 x Waracle Headset"
    When I open the cart
    And I apply the coupon "WARACLE25"
    And the order summary shows a coupon line for "WARACLE25"
    And I apply an empty coupon code
    Then no discount is applied
    And the order summary shows:
      | Subtotal | Discount | Shipping | Total  |
      | £59.99   | none     | £5.00    | £64.99 |

  @Edge
  Scenario: AC-4 – Removing the last item empties the basket and its summary
    Given my basket contains "1 x Waracle Cap"
    When I open the cart
    And I apply the coupon "WARACLE25"
    And the order summary shows a coupon line for "WARACLE25"
    And I remove "Waracle Cap" from the cart
    Then the cart shows the empty basket message
    And no order summary is shown
