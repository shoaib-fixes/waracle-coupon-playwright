@AC6
Feature: Order confirmation reflects the coupon
  AC-6  The order confirmation shows the applied coupon, the discount and the final total.

  Background:
    Given I am signed in as the demo customer

  @Positive @Smoke
  Scenario: AC-6 – The confirmation shows the applied coupon, its discount and the total paid
    Given I am at the checkout with "2 x Waracle Headset" and the coupon "WARACLE25" applied
    When I complete checkout with sample shipping and card details
    Then the order confirmation is shown
    And the order confirmation shows the coupon "WARACLE25"
    And the order confirmation shows the same discount and total as the checkout summary

  @Positive @KnownDefect
  Scenario: AC-6 – The amounts on the confirmation follow the release pricing rules
    Given I am at the checkout with "2 x Waracle Headset" and the coupon "WARACLE25" applied
    When I complete checkout with sample shipping and card details
    Then the order confirmation is shown
    And the order confirmation is priced according to the release rules

  @Negative
  Scenario: AC-6 – An order placed without a coupon shows no coupon line
    Given I am at the checkout with "1 x Waracle Cap" and no coupon
    When I complete checkout with sample shipping and card details
    Then the order confirmation is shown
    And the order confirmation shows no coupon line
    And the total paid is £24.99

  @Edge
  Scenario: AC-6 – The amount on the Pay button is the amount shown as paid
    Given I am at the checkout with "1 x Waracle Headset" and the coupon "WARACLE25" applied
    When I complete checkout with sample shipping and card details
    Then the order confirmation is shown
    And the total paid equals the amount that was on the Pay button
