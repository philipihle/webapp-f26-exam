import { useNavigate, useParams } from 'react-router-dom';
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
    <div className="text-start">
      <h2>Update Room</h2>
      <RoomForm
        onRoomChanged={(room) => mutation.mutateAsync(room)}
        roomId={roomQuery.data.id}
        initialValues={roomQuery.data}
        error={mutation.error instanceof Error ? mutation.error.message : null}
        submitting={mutation.isPending}
      />
    </div>
  );
}
