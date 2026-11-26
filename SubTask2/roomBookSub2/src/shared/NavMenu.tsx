import { Nav, Navbar, Container } from 'react-bootstrap';
import { Link } from 'react-router-dom';

// Top navigation bar, same markup and classes as _Layout.cshtml in SubTask1.
// react-bootstrap's Navbar handles the mobile toggle (instead of Bootstrap's jQuery script).
export default function NavMenu() {
  return (
    <header className="sticky-top bg-white border-bottom shadow-sm">
      <Navbar expand="lg" className="navbar-light py-2">
        <Container fluid className="px-3 px-lg-4">
          <Navbar.Brand as={Link} to="/" className="d-flex align-items-center gap-2 m-0">
            <span className="fw-bold">BookingPortal</span>
          </Navbar.Brand>
          <Navbar.Toggle aria-controls="navbarContent" className="border-0" />
          <Navbar.Collapse id="navbarContent">
            <Nav className="me-auto mb-2 mb-lg-0 align-items-lg-center ms-lg-3 gap-1">
              <Nav.Link as={Link} to="/rooms" className="px-3 py-2 text-dark d-inline-flex align-items-center gap-1">
                <span>Find a room</span>
              </Nav.Link>
            </Nav>
          </Navbar.Collapse>
        </Container>
      </Navbar>
    </header>
  );
}
