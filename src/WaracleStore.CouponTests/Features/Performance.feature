@Performance
Feature: Performance of the coupon flow
  Smoke-level budgets that catch a regression in the coupon journey. This is not a load test.
  Budgets live in appsettings.json → Budgets and can be overridden per environment
  (for example TEST_Budgets__PageLoadMs=6000 on a slow CI runner). Every measurement is
  recorded on the result so trends are visible in the report.

  Scenario: The cart page with a basket loads within budget
    Given I am signed in as the demo customer
    And my basket contains "2 x Waracle Headset, 1 x Waracle Cap"
    When I open the cart and record its load timings
    Then the cart page loads within the performance budget

  Scenario: Applying a coupon re-prices the summary within budget
    Given I am signed in as the demo customer
    And my basket contains "2 x Waracle Headset, 1 x Waracle Cap"
    When I open the cart
    And I apply the coupon "WARACLE25" and time how long the summary takes to update
    Then the coupon is reflected within the performance budget

  @API
  Scenario: The cart summary endpoint answers within budget under light repetition
    When I request the cart summary for "2 x Waracle Headset, 1 x Waracle Cap" with coupon "WARACLE25" 25 times
    Then the 95th percentile response time is within the performance budget
