
package common;

public class MessageFactory {
    public MessageFactory() { }
    public Message createMessage(MsgType type) {
        switch(type) {
            case MSG_DISCONNECT:
                return new DisconnectMessage();
            case MSG_LIST:
                return new ListMessage();
            default:
                assert false: "invalid message structure: " + type;
                return null;
        }
    }
    public Message createMessage(MsgType type, String destination, String body) {
        switch(type) {
            case MSG_CHAT:
                ChatMessage msg = new ChatMessage();
                msg.mSource = "<me>";
                msg.mDestination = destination;
                msg.mBody = body;
                return msg;
            case MSG_BROADCAST:
                BroadcastMessage bmsg = new BroadcastMessage();
                bmsg.mSource = "<me>";
                bmsg.mDestination = "<everyone>";
                bmsg.mBody = body;
                return bmsg;
            default:
                assert false: "invalid message structure: " + type;
                return null;
        }
    }
    public Message createMessage(MsgType type, String name) {
        switch(type) {
            case MSG_HANDSHAKE:
                HandshakeMessage msg = new HandshakeMessage();
                msg.name = name;
                return msg;
            default:
                assert false: "invalid message structure: " + type;
                return null;
        }
    }
}