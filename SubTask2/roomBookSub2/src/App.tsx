import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import Container from 'react-bootstrap/Container';
import RoomListPage from './rooms/RoomListPage';
import RoomCreatePage from './rooms/RoomCreatePage';
import RoomUpdatePage from './rooms/RoomUpdatePage';

const queryClient = new QueryClient(); // one shared instance for the whole app

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <Router>
        <Container className="mt-4">
          <Routes>
            <Route path="/rooms" element={<RoomListPage />} />
            <Route path="/roomcreate" element={<RoomCreatePage />} />
            <Route path="/roomupdate/:roomId" element={<RoomUpdatePage />} />
            <Route path="*" element={<Navigate to="/rooms" replace />} />
          </Routes>
        </Container>
      </Router>
    </QueryClientProvider>
  );
}
