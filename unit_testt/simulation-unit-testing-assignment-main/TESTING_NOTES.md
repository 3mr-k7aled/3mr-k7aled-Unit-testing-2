
## Testing Frameworks and Libraries

The project uses the following testing libraries =>
1 - xUnit => Test framework
2 - Moq => Mocking dependencies
3 - FluentAssertions => Readable test assertions
4 - Verify.Xunit => Snapshot testing
5 - NetArchTest => Architecture testing
6 - Coverlet => Code coverage collection

## Unit Tests

The CheckoutService tests cover the following scenarios =>
1  - Null order validation
2  - Empty order validation
3  - Invalid quantity validation
4  - Negative price validation
5  - Insufficient inventory
6  - Successful checkout
7  - Subtotal and total calculations
8  - Discount calculation
9  - Discount threshold
10 - Maximum discount limit
11 - Order repository interaction
12 - Notification service interaction
13 - Preventing save and notification when validation fails
14 - Constructor dependency validation
15 - Cancellation token propagation

## Snapshot Testing

A snapshot test verifies that the generated CheckoutReceipt matches the approved snapshot Verify Xunit is used to detect 
unexpected changes in the receipt structure and values

##  Architecture Tests

Architecture tests ensure that => The Domain layer does not depend on the Application layer The Domain layer 
does not depend on the Infrastructure layer

##  Test Results

 Total tests => 21
 Passed tests => 21
 Failed tests => 0

## Code Coverage

Visual Studio Code Coverage reported
Coverage metric 1 => 97.11%
Coverage metric 2 => 88.08%
Both reported coverage values are above 80% as I asked to do and I will put screen shoot of specific coverage result

