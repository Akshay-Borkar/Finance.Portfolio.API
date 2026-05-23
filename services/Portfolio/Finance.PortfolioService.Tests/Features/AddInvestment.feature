Feature: Add Investment to a Stock
  As a portfolio owner
  I want to record an investment I made in a stock
  So that I can track how much I have invested and at what price

  Background:
    Given a user with id "11111111-1111-1111-1111-111111111111"

  Scenario: Successfully add an investment to an owned stock
    Given a stock with id "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" owned by the current user
    When I add an investment of 5000 at a buying price of 180.00 on "2025-01-15"
    Then a new investment should be created
    And the investment should have an amount of 5000
    And the investment should have a buying price of 180.00
    And the returned investment id should not be empty

  Scenario: Cannot add an investment to a stock that does not exist
    Given no stock exists with id "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
    When I try to add an investment of 1000 at a buying price of 100.00 to that stock
    Then an unauthorized access error should be raised

  Scenario: Cannot add an investment to a stock owned by another user
    Given a stock with id "cccccccc-cccc-cccc-cccc-cccccccccccc" owned by a different user
    When I try to add an investment of 2000 at a buying price of 200.00 to that stock
    Then an unauthorized access error should be raised
