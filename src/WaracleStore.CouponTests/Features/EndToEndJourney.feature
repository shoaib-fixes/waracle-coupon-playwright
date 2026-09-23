@AC1 @AC6 @Journey
Feature: Customer journey with a coupon
  One scenario that takes the real customer path end to end: sign in through the login
  form, pick a product from the catalogue, apply the coupon in the cart, pay, and read the
  confirmation. Every other feature seeds the session and basket directly to stay fast and
  focused; this one proves the seeded state matches what the UI itself produces.

  @Positive @Smoke
  Scenario: A signed-in customer applies WARACLE25 to a basket built from the catalogue
    Given I am on the login page
    When I sign in with the demo credentials
    Then I am greeted as "John"
    When I add 2 x "Waracle Cap" to the cart from its product page
    And I open the cart
    And I apply the coupon "WARACLE25"
    Then the cart confirms the coupon "WARACLE25" is applied
    When I proceed to checkout
    And I complete checkout with sample shipping and card details
    Then the order confirmation is shown
    And the order confirmation shows the coupon "WARACLE25"
    And the order confirmation shows the same discount and total as the checkout summary
