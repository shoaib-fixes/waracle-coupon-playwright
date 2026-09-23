@AC5
Feature: Invalid and empty coupon codes
  AC-5  An invalid or empty code applies no discount and shows a clear message.

  The two halves of AC-5 are checked in separate scenarios so that the "no discount" half
  reports independently of the "clear message" half.

  Background:
    Given I am signed in as the demo customer
    And my basket contains "1 x Waracle Headset"

  @Negative
  Scenario Outline: AC-5 – An invalid code applies no discount
    When I open the cart
    And I apply the coupon "<Code>"
    Then no discount is applied
    And the order summary shows:
      | Subtotal | Discount | Shipping | Total  |
      | £59.99   | none     | £5.00    | £64.99 |

    Examples:
      | Code           |
      | WARACLE50      |
      | WARACLE-25     |
      | WARACLE25EXTRA |
      | SAVE25         |
      | ' OR 1=1 --    |

  @Negative @KnownDefect
  Scenario Outline: AC-5 – An invalid code shows a clear message
    When I open the cart
    And I apply the coupon "<Code>"
    Then I am shown a clear message that the coupon was not accepted

    Examples:
      | Code      |
      | WARACLE50 |
      | SAVE25    |

  @Negative
  Scenario: AC-5 – An empty code applies no discount
    When I open the cart
    And I apply an empty coupon code
    Then no discount is applied
    And the order summary is priced according to the release rules

  @Negative @KnownDefect
  Scenario: AC-5 – An empty code shows a clear message
    When I open the cart
    And I apply an empty coupon code
    Then I am shown a clear message that the coupon was not accepted

  @Negative @KnownDefect
  Scenario: AC-5 – A code made only of spaces shows a clear message
    When I open the cart
    And I apply a coupon code made only of spaces
    Then no discount is applied
    And I am shown a clear message that the coupon was not accepted

  @Edge
  Scenario: AC-5 – An invalid code entered after a valid one removes the discount
    When I open the cart
    And I apply the coupon "WARACLE25"
    And the order summary shows a coupon line for "WARACLE25"
    And I apply the coupon "WARACLE50"
    Then no discount is applied
    And the order summary is priced according to the release rules

  @Edge
  Scenario: AC-5 – A very long code is rejected without breaking the cart
    When I open the cart
    And I apply a coupon code of 500 characters
    Then no discount is applied
    And the cart is still usable
