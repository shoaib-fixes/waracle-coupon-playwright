@Accessibility
Feature: Accessibility of the coupon screens
  Every screen the coupon feature touches is scanned with axe-core against WCAG 2.1 A and AA.
  A scenario fails on serious or critical violations; the full scan, including moderate and
  minor findings, is attached to the result.

  All three screens currently fail on one serious rule, colour contrast (grey helper text on
  white), so they are tagged @KnownDefect – see docs/ReleaseObservations.md, D10.

  Background:
    Given I am signed in as the demo customer

  @KnownDefect
  Scenario: The cart with a coupon applied has no serious or critical WCAG 2.1 AA violations
    Given my basket contains "1 x Waracle Headset"
    When I open the cart
    And I apply the coupon "WARACLE25"
    And the order summary shows a coupon line for "WARACLE25"
    Then the page has no serious or critical WCAG 2.1 AA violations

  @KnownDefect
  Scenario: The checkout has no serious or critical WCAG 2.1 AA violations
    Given I am at the checkout with "1 x Waracle Headset" and the coupon "WARACLE25" applied
    Then the page has no serious or critical WCAG 2.1 AA violations

  @KnownDefect
  Scenario: The order confirmation has no serious or critical WCAG 2.1 AA violations
    Given I am at the checkout with "1 x Waracle Headset" and the coupon "WARACLE25" applied
    When I complete checkout with sample shipping and card details
    Then the order confirmation is shown
    And the page has no serious or critical WCAG 2.1 AA violations
