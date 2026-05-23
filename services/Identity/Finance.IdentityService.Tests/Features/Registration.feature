Feature: User Registration
  As a new user
  I want to register an account
  So that I can start tracking my investment portfolio

  Scenario: Successfully register a new user
    When I register with first name "Jane" last name "Doe" username "janedoe" email "jane@example.com" password "SecurePass1!" and role "User"
    Then the registration should succeed
    And the returned user id should not be empty

  Scenario: Registration always sets email as confirmed
    When I register with first name "Bob" last name "Smith" username "bobsmith" email "bob@example.com" password "Pass123!" and role "User"
    Then the created user should have email confirmed set to true

  Scenario: Registration fails when identity validation rejects the password
    Given the identity system rejects the password with error "Password too short."
    When I try to register with username "baduser" email "bad@example.com" password "weak" and role "User"
    Then a bad request error should be raised during registration
    And the error should mention "Password too short."

  Scenario: Registration assigns the user to the correct role
    When I register with first name "Admin" last name "User" username "adminuser" email "admin@example.com" password "Admin123!" and role "Admin"
    Then the user should be added to the role "Admin"
