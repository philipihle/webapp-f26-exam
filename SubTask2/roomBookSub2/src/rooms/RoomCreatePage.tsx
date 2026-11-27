import { Link, useNavigate } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import RoomForm from './RoomForm';
import * as RoomService from './RoomService';

export default function RoomCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient(); // the same instance as in App.tsx

  const mutation = useMutation({
    mutationFn: RoomService.createRoom,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] });
      navigate('/rooms'); // back to the room list
    },
  });

  return (
    <div className="py-4 text-start">
      <div className="mb-4">
        <Link to="/rooms" className="btn btn-outline-secondary btn-sm">Back to all rooms</Link>
      </div>
      <div className="card border-0 shadow-sm rounded-4 p-4 p-md-5">
        <h4 className="fw-bold mb-1">Create Room</h4>
        <p className="text-muted small mb-4">Fill in the details for the new room.</p>
        <RoomForm
          onRoomChanged={(room) => mutation.mutateAsync(room)}
          error={mutation.error instanceof Error ? mutation.error.message : null}
          submitting={mutation.isPending}
        />
      </div>
    </div>
  );
}
