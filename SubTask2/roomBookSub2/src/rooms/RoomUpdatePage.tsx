import { Link, useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import RoomForm from './RoomForm';
import * as RoomService from './RoomService';

export default function RoomUpdatePage() {
  const { roomId } = useParams<{ roomId: string }>(); // id from the URL /roomupdate/:roomId
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  // Load the room so the form can be filled in
  const roomQuery = useQuery({
    queryKey: ['rooms', roomId],
    queryFn: () => RoomService.fetchRoomById(roomId!),
    enabled: !!roomId,
  });

  const mutation = useMutation({
    mutationFn: RoomService.updateRoom,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] });
      navigate('/rooms');
    },
  });

  if (roomQuery.isLoading) return <p>Loading...</p>;
  if (roomQuery.isError) return <p className="text-danger">Failed to load room</p>;
  if (!roomQuery.data) return <p>No room found</p>;

  return (
    <div className="py-4 text-start">
      <div className="mb-4">
        <Link to="/rooms" className="btn btn-outline-secondary btn-sm">Back to all rooms</Link>
      </div>
      <div className="card border-0 shadow-sm rounded-4 p-4 p-md-5">
        <h4 className="fw-bold mb-1">Update Room</h4>
        <p className="text-muted small mb-4">Change the details for {roomQuery.data.name}.</p>
        <RoomForm
          onRoomChanged={(room) => mutation.mutateAsync(room)}
          roomId={roomQuery.data.id}
          initialValues={roomQuery.data}
          error={mutation.error instanceof Error ? mutation.error.message : null}
          submitting={mutation.isPending}
        />
      </div>
    </div>
  );
}
