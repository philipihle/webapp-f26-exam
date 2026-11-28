import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Modal, Form, Button, Alert } from 'react-bootstrap';
import type { Booking } from '../types/booking';
import type { BookingChat } from '../types/bookingchat';
import { useAuth } from '../auth/AuthContext';
import * as ChatService from './BookingChatService';

interface Props {
  booking: Booking | null;
  onHide: () => void;
}

export default function BookingChatModal({ booking, onHide }: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [message, setMessage] = useState('');

  const { data: chats = [], isLoading, error } = useQuery<BookingChat[], Error>({
    queryKey: ['bookingchats', booking?.id],
    queryFn: () => ChatService.getBookingChats(booking!.id!),
    enabled: booking !== null,
    refetchInterval: 5000, // Fetching new messages every 5 seconds for a live chat experience
  });

  const sendMutation = useMutation({
    mutationFn: (text: string) => ChatService.sendBookingChat(booking!.id!, text),
    onSuccess: () => {
      setMessage('');
     queryClient.invalidateQueries({ queryKey: ['bookingchats', booking?.id] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (chatId: number) => ChatService.deleteBookingChat(chatId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bookingchats', booking?.id] });
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!message.trim() || !booking?.id) return;
    sendMutation.mutate(message.trim());
  };

  return (
    <Modal show={booking !== null} onHide={onHide} centered size="lg">
      <Modal.Header closeButton>
        <Modal.Title className="h5 fw-bold">Chat: {booking?.roomName}</Modal.Title>
      </Modal.Header>

      <Modal.Body>
        {error && <Alert variant="danger">Failed to load messages.</Alert>}
        {sendMutation.error && <Alert variant="danger">{sendMutation.error.message}</Alert>}
        {deleteMutation.error && <Alert variant="danger">{deleteMutation.error.message}</Alert>}

        <div className="d-flex flex-column gap-2 mb-3" style={{ maxHeight: '500px', overflowY: 'auto' }}>
          {isLoading && <p className="text-muted small mb-0">Loading...</p>}
          {!isLoading && chats.length === 0 && (
            <p className="text-muted small text-center my-3">No messages yet. Start the conversation!</p>
          )}
          {chats.map(c => {
            const isMe = c.userId === user?.nameid;
            const time = new Date(c.timestamp).toLocaleTimeString('nb-NO', { hour: '2-digit', minute: '2-digit' });
            return (
              <div key={c.id} className={`d-flex flex-column ${isMe ? 'align-items-end' : 'align-items-start'}`}>
                <small className="text-muted mb-1">
                  {isMe ? 'You' : c.userName} &bull; {time}
                  {(isMe || user?.role === 'Admin') && (
                    <Button variant="link" size="sm" className="text-danger p-0 ms-2 text-decoration-none"
                      onClick={() => deleteMutation.mutate(c.id)} disabled={deleteMutation.isPending}>
                      Delete
                    </Button>
                  )}
                </small>
                <div className={`rounded-3 px-3 py-2 text-break ${isMe ? 'bg-primary text-white' : 'bg-light'}`} style={{ maxWidth: '80%' }}>
                  {c.message}
                </div>
              </div>
            );
          })}
        </div>

        <Form onSubmit={handleSubmit} className="d-flex gap-2 pt-3 border-top">
          <Form.Control type="text" placeholder="Write a message..." value={message}
            onChange={e => setMessage(e.target.value)} maxLength={500} required />
          <Button type="submit" disabled={sendMutation.isPending}>Send</Button>
        </Form>
      </Modal.Body>
    </Modal>
  );
}
