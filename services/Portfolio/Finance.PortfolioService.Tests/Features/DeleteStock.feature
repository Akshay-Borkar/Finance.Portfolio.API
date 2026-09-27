Feature: Delete Stock from Portfolio
  As a portfolio owner
  I want to delete a stock from my portfolio
  So that stocks I no longer hold are removed along with their investment history

  The investment rows go with the stock through the ON DELETE CASCADE on
  Investments.StockDetailsId, so this is a single atomic delete. It used to be a loop that
  called the repository once per investment, and since every repository mutator commits on
  its own that ran N+1 separate transactions.

  Background:
    Given a user with id "11111111-1111-1111-1111-111111111111"

  Scenario: Successfully delete a stock
    Given a stock with id "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" and ticker "AAPL" owned by the current user
    When I delete the stock
    Then the stock should be deleted exactly once
    And a StockRemoved event should be published for ticker "AAPL"

  Scenario: Deleting a stock does not issue per-investment deletes
    Given a stock with id "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb" and ticker "META" owned by the current user
    When I delete the stock
    Then the stock should be deleted exactly once
    And the investment repository should not be used at all

  Scenario: Cannot delete a stock that does not exist
    Given no stock exists with id "cccccccc-cccc-cccc-cccc-cccccccccccc"
    When I try to delete that stock
    Then a not found error should be raised

  Scenario: Cannot delete a stock owned by another user
    Given a stock with id "dddddddd-dddd-dddd-dddd-dddddddddddd" and ticker "GOOG" owned by a different user
    When I try to delete that stock
    Then a bad request error should be raised with message "permission"
