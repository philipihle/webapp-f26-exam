import { Nav, Navbar, Container } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import AuthSection from '../auth/AuthSection';
import { useAuth } from '../auth/AuthContext';

// Top navigation bar, same markup and classes as _Layout.cshtml in SubTask1.
// react-bootstrap's Navbar handles the mobile toggle (instead of Bootstrap's jQuery script).
export default function NavMenu() {
  const { user } = useAuth(); // "My Bookings" is only shown to logged-in users

  return (
    <header className="sticky-top bg-white border-bottom shadow-sm">
      <Navbar expand="lg" className="navbar-light py-2">
        <Container fluid className="px-4">
          <Navbar.Brand as={Link} to="/" className="fw-bold">
            BookingPortal
          </Navbar.Brand>
          <Navbar.Toggle aria-controls="navbarContent" className="border-0" />
          <Navbar.Collapse id="navbarContent">
            {user && (
              <Nav className="me-auto ms-lg-3">
                <Nav.Link as={Link} to="/rooms" className="text-dark">
                  Find a room
                </Nav.Link>
                <Nav.Link as={Link} to="/mybookings" className="text-dark">
                  My Bookings
                </Nav.Link>
              </Nav>
            )}
            <AuthSection />
          </Navbar.Collapse>
        </Container>
      </Navbar>
    </header>
  );
}
