Feature: Delete Stock Price Alert
  As a registered user
  I want to delete one of my price alerts
  So that I stop receiving notifications for it

  Background:
    Given an alert user with id "33333333-3333-3333-3333-333333333333"

  Scenario: Successfully delete an alert that belongs to the user
    Given an alert with id "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" belonging to the current user
    When I delete alert "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
    Then the alert should be deleted from the repository

  Scenario: Silently ignores deleting an alert that does not exist
    Given no alert exists with id "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
    When I delete alert "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
    Then no alert should be deleted

  Scenario: Cannot delete an alert that belongs to another user
    Given an alert with id "cccccccc-cccc-cccc-cccc-cccccccccccc" belonging to a different user
    When I delete alert "cccccccc-cccc-cccc-cccc-cccccccccccc"
    Then no alert should be deleted
