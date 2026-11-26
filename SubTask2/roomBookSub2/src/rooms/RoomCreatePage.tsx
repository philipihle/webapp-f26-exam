import { useNavigate } from 'react-router-dom';
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
    <div className="text-start">
      <h2>Create Room</h2>
      <RoomForm
        onRoomChanged={(room) => mutation.mutateAsync(room)}
        error={mutation.error instanceof Error ? mutation.error.message : null}
        submitting={mutation.isPending}
      />
    </div>
  );
}
