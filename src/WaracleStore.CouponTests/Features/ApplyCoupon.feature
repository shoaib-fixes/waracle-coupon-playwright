@AC1 @AC2
Feature: Apply a coupon code in the cart
  WS-101 – As a customer, I want to apply a coupon code to my basket so that I get
  the discount before I pay. The release launches one code, WARACLE25.

  AC-1  A customer can apply a coupon code from the cart.
  AC-2  Applying WARACLE25 reduces the subtotal by 25%.

  Background:
    Given I am signed in as the demo customer

  @Positive @Smoke
  Scenario: AC-1 – The launch coupon can be applied from the cart
    Given my basket contains:
      | Product         | Quantity |
      | Waracle Headset | 1        |
    When I open the cart
    And I apply the coupon "WARACLE25"
    Then the cart confirms the coupon "WARACLE25" is applied
    And the order summary shows a coupon line for "WARACLE25"

  @Positive
  Scenario: AC-1 – The coupon applied in the cart carries through to the checkout summary
    Given my basket contains "1 x Waracle Cap"
    When I open the cart
    And I apply the coupon "WARACLE25"
    And the order summary shows a coupon line for "WARACLE25"
    And I proceed to checkout
    Then the order summary shows a coupon line for "WARACLE25"

  @Positive @KnownDefect
  Scenario Outline: AC-2 – WARACLE25 reduces the subtotal by 25%
    Given my basket contains "<Basket>"
    When I open the cart
    And I apply the coupon "WARACLE25"
    Then the order summary shows:
      | Subtotal   | Discount   | Shipping | Total   |
      | <Subtotal> | <Discount> | £5.00    | <Total> |

    Examples:
      | Basket                               | Subtotal | Discount | Total   |
      | 1 x Waracle Cap                      | £19.99   | £5.00    | £19.99  |
      | 3 x Men's Logo T-Shirt               | £74.97   | £18.74   | £61.23  |
      | 2 x Waracle Headset, 1 x Waracle Cap | £139.97  | £34.99   | £109.98 |

  @Edge @KnownDefect
  Scenario: AC-2 – The discount is recalculated when the quantity changes after the coupon is applied
    Given my basket contains "1 x Waracle Headset"
    When I open the cart
    And I apply the coupon "WARACLE25"
    And the order summary shows a coupon line for "WARACLE25"
    And I increase the quantity of "Waracle Headset"
    Then the order summary is priced according to the release rules

  # The acceptance criteria name the code in upper case only; the release accepts it in any
  # case and ignores surrounding spaces. These scenarios pin that observed behaviour so that
  # any change to it is deliberate (see README → "Observations on the release").
  @Edge
  Scenario Outline: AC-2 – The launch code is accepted regardless of letter case
    Given my basket contains "1 x Waracle Cap"
    When I open the cart
    And I apply the coupon "<Entered>"
    Then the cart confirms the coupon is applied
    And the order summary shows a coupon line

    Examples:
      | Entered   |
      | waracle25 |
      | Waracle25 |

  @Edge
  Scenario: AC-2 – Spaces around the launch code are ignored
    Given my basket contains "1 x Waracle Cap"
    When I open the cart
    And I apply the coupon "WARACLE25" surrounded by spaces
    Then the cart confirms the coupon is applied
    And the order summary shows a coupon line
