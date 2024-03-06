using System;
using HotelBooking.Core;
using HotelBooking.UnitTests.Fakes;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using Moq;
using HotelBooking.Infrastructure.Repositories;


namespace HotelBooking.UnitTests
{
    public class BookingManagerTests
    {
        private IBookingManager bookingManager;
        new Mock<IRepository<Booking>> mockBookingRepository;
        new Mock<IRepository<Room>> mockRoomRepository;

        public BookingManagerTests(){
            // Setup a mock object of IRepository<Booking> & IRepository<Room> to use in the tests using the moq framework
            mockRoomRepository = new Mock<IRepository<Room>>();
            mockBookingRepository = new Mock<IRepository<Booking>>();

            bookingManager = new BookingManager(mockBookingRepository.Object, mockRoomRepository.Object);
        }

        [Fact]
        public void GetFullyOccupiedDates_WithStartDateLaterThanEndDate_ShouldThrowArgumentException()
        {
            // Arrange
            DateTime startDate = DateTime.Today.AddDays(1);
            DateTime endDate = DateTime.Today;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => bookingManager.GetFullyOccupiedDates(startDate, endDate));
            Assert.Contains("The start date cannot be later than the end date.", exception.Message);
        }

        [Fact]
        public void GetFullyOccupiedDates_WithNoBookings_ShouldReturnEmptyList()
        {
            // Arrange
            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today.AddDays(7);
            mockBookingRepository.Setup(b => b.GetAll()).Returns(new List<Booking>());
            mockRoomRepository.Setup(r => r.GetAll()).Returns(new List<Room>());

            // Act
            var result = bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetFullyOccupiedDates_WithFullyOccupiedDates_ShouldReturnOccupiedDates()
        {
            // Arrange
            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today.AddDays(2);

            DateTime start = DateTime.Today.AddDays(10);
            DateTime end = DateTime.Today.AddDays(20);
            mockBookingRepository.Setup(m => m.GetAll()).Returns(new List<Booking>
            {
                new Booking { Id=1, StartDate=DateTime.Today.AddDays(6), EndDate=DateTime.Today.AddDays(6), IsActive=true, CustomerId=1, RoomId=1 },
                new Booking { Id=2, StartDate=start, EndDate=end, IsActive=true, CustomerId=1, RoomId=1 },
                new Booking { Id=3, StartDate=start, EndDate=end, IsActive=true, CustomerId=2, RoomId=2 },
            });

            // Act
            var result = bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Equal(3, result.Count); // Assuming inclusive dates
            Assert.Contains(startDate, result);
            Assert.Contains(startDate.AddDays(1), result);
            Assert.Contains(endDate, result);
        }

        [Fact]
        public void GetFullyOccupiedDates_WithNoFullyOccupiedDates_ShouldReturnEmptyList()
        {
            // Arrange
            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today.AddDays(2);
            mockRoomRepository.Setup(r => r.GetAll()).Returns(new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } });
            mockBookingRepository.Setup(b => b.GetAll()).Returns(new List<Booking>
            {
                new Booking { RoomId = 1, StartDate = startDate, EndDate = endDate, IsActive = true }
                // Only one room booked, so no fully occupied dates
            });

            // Act
            var result = bookingManager.GetFullyOccupiedDates(startDate, endDate);

            // Assert
            Assert.Empty(result);
        }

        // Write a test for BookingManager.CreateBooking that makes it throw an exception if the start date is in the past.
        [Fact]
        public void CreateBooking_StartDateInThePast_ThrowsArgumentException()
        {
            // Arrange
            DateTime date = DateTime.Today;
            Booking booking = new Booking
            {
                StartDate = date,
                EndDate = date.AddDays(1),
                IsActive = true
            };

            // Act
            Action act = () => bookingManager.CreateBooking(booking);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }

        // Write a data driven test for BookingManager.CreateBooking that tests the following test case:
        // The room is available, i.e. there are no active bookings that overlap with the new booking.
        [Fact]
        public void CreateBooking_RoomAvailable_ReturnsTrue()
        {
            // Arrange
            DateTime date = DateTime.Today.AddDays(1);
            Booking booking = new Booking
            {
                StartDate = date,
                EndDate = date.AddDays(1),
                IsActive = true
            };

            mockRoomRepository.Setup(m => m.GetAll()).Returns(new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } });
            mockBookingRepository.Setup(m => m.GetAll()).Returns(new List<Booking>());

            // Act
            bool result = bookingManager.CreateBooking(booking);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void FindAvailableRoom_StartDateNotInTheFuture_ThrowsArgumentException()
        {
            // Arrange
            DateTime date = DateTime.Today;

            // Act
            Action act = () => bookingManager.FindAvailableRoom(date, date);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }

        [Fact]
        public void FindAvailableRoom_RoomAvailable_RoomIdNotMinusOne()
        {
            // Arrange
            DateTime date = DateTime.Today.AddDays(2);

            mockRoomRepository.Setup(m => m.GetAll()).Returns(new List<Room> { new Room { Id = 1 }, new Room { Id = 2 } });
            mockBookingRepository.Setup(m => m.GetAll()).Returns(new List<Booking>());

            // Act
            int roomId = bookingManager.FindAvailableRoom(date, date);

            // Assert
            Assert.NotEqual(-1, roomId);
        }

        [Fact]
        public void FindAvailableRoom_RoomAvailable_ReturnsAvailableRoom()
        {
            // This test was added to satisfy the following test design
            // principle: "Tests should have strong assertions".

            // Arrange
            DateTime date = DateTime.Today.AddDays(1);
            // Act
            int roomId = bookingManager.FindAvailableRoom(date, date);

            // Assert
            var bookingForReturnedRoomId = mockBookingRepository.Object.GetAll().Where(
                b => b.RoomId == roomId
                && b.StartDate <= date
                && b.EndDate >= date
                && b.IsActive);

            Assert.Empty(bookingForReturnedRoomId);
        }
    }
}
